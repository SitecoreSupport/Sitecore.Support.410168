// © 2016 Sitecore Corporation A/S. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.Analytics.Aggregation.Data.Model;
using Sitecore.Analytics.Model;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using Sitecore.EmailCampaign.ExperienceAnalytics;
using Sitecore.EmailCampaign.ExperienceAnalytics.Dimensions;
using Sitecore.EmailCampaign.ExperienceAnalytics.Properties;
using Sitecore.EmailCampaign.Model;
using Sitecore.EmailCampaign.Model.XConnect;
using Sitecore.EmailCampaign.Model.XConnect.Events;
using Sitecore.EmailCampaign.Model.XConnect.Facets;
using Sitecore.EmailCampaign.XConnect.Web;
using Sitecore.ExM.Framework.Diagnostics;
using Sitecore.ExperienceAnalytics.Aggregation.Data.Model;
using Sitecore.ExperienceAnalytics.Aggregation.Data.Schema;
using Sitecore.ExperienceAnalytics.Aggregation.Dimensions;
using Sitecore.ExperienceAnalytics.Core;
using Sitecore.Framework.Conditions;
using Sitecore.XConnect;
using Sitecore.XConnect.Client;
using Sitecore.XConnect.Collection.Model;

namespace Sitecore.Support.EmailCampaign.ExperienceAnalytics.Dimensions
{
    /// <summary>
    /// An abstract implementation of base class for each derived dimension.
    /// </summary>
    public abstract class ExmDimensionBase : DimensionBase
    {
        internal readonly ILogger Logger;
        private readonly IUniqueEventCache _uniqueEventCache;
        private readonly XConnectRetry _xConnectRetry;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExmDimensionBase"/> class.
        /// </summary>
        /// <param name="dimensionId">Dimension's ID.</param>
        protected ExmDimensionBase(Guid dimensionId)
            : this(ServiceLocator.ServiceProvider.GetService<ILogger>(), ServiceLocator.ServiceProvider.GetService<IUniqueEventCache>(), ServiceLocator.ServiceProvider.GetService<XConnectRetry>(), dimensionId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExmDimensionBase"/> class.
        /// </summary>
        /// <param name="logger">Logger for current dimension.</param>
        /// <param name="uniqueEventCache">The cache for unique events</param>
        /// <param name="xConnectRetry">The xConnect retry operation helper</param>
        /// <param name="dimensionId">Dimension's ID.</param>
        protected ExmDimensionBase([NotNull] ILogger logger, IUniqueEventCache uniqueEventCache, XConnectRetry xConnectRetry, Guid dimensionId)
            : base(dimensionId)
        {
            Condition.Requires(logger, nameof(logger)).IsNotNull();
            Condition.Requires(uniqueEventCache, nameof(uniqueEventCache)).IsNotNull();
            Condition.Requires(xConnectRetry, nameof(xConnectRetry)).IsNotNull();

            Logger = logger;
            _uniqueEventCache = uniqueEventCache;
            _xConnectRetry = xConnectRetry;
        }

        /// <summary>
        /// Retrieves the collection of <see cref="DimensionData"/> for particular <see cref="IVisitAggregationContext"/>.
        /// </summary>
        /// <param name="context">Context which contains a <see cref="VisitData"/> information.</param>
        /// <returns>Collection of <see cref="DimensionData"/> for visit.</returns>
        public override IEnumerable<DimensionData> GetData([NotNull] IVisitAggregationContext context)
        {
            Assert.ArgumentNotNull(context, "context");

            var dimensions = new List<DimensionData>();

            if (context.Visit?.CustomValues == null || !context.Visit.CustomValues.Any())
            {
                return dimensions;
            }

            VisitData visit = context.Visit;

            Interaction interaction = null;

            if (!(context.Visit.CustomValues.First().Value is Interaction))
            {
                return dimensions;
            }
            interaction = context.Visit.CustomValues.First().Value as Interaction;
            Logger.LogDebug($"Processing {GetType()} dimension on Interaction {visit.InteractionId}");

            try
            {
                dimensions = GetDimensions(interaction).ToList();
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

            Logger.LogDebug($"{GetType()} returned {dimensions.Count} dimensions on Interaction {visit.InteractionId}");
            return dimensions;
        }

        /// <summary>
        /// Gets collection of <see cref="DimensionData"/> for interactions with one or more <see cref="EmailEvent"/>
        /// </summary>
        /// <param name="interaction">Interaction that contains information to proceed.</param>
        protected virtual IEnumerable<DimensionData> GetDimensions(Interaction interaction)
        {
            Assert.ArgumentNotNull(interaction, nameof(interaction));

            List<EmailEvent> events = interaction.Events.OfType<EmailEvent>().ToList();
            if (!events.Any())
            {
                Logger.LogDebug("No EXM events found");
                yield break;
            }

            // Get EXM events and ignore all other events
            List<EmailEvent> exmEvents = interaction.Events.OfType<EmailEvent>().ToList();

            foreach (EmailEvent emailEvent in exmEvents)
            {
                // Determine the email event type. 
                EmailEventType emailEventType = DimensionUtils.ParsePageEvent(emailEvent.DefinitionId);

                // Dimensions can only be created if a CustomKey is resolved
                string dimensionKey = CreateDimensionKey(interaction, emailEvent, emailEventType);

                if (dimensionKey == null)
                {
                    continue;
                }

                // Create a dimension for each EXM event
                var dimension = new DimensionData
                {
                    DimensionKey = dimensionKey,
                    MetricsValue = new SegmentMetricsValue
                    {
                        Visits = 1,
                        PageViews = 1
                    }
                };

                // Find the next EXM event in the interaction
                // As events in the interaction are ordered by the timestamp, this allow us to filter 
                // which events are related to the current EXM event we are processing e.g. goals and any browsed pages
                EmailEvent nextExmEvent = exmEvents.FirstOrDefault(x => x.Timestamp > emailEvent.Timestamp);
                DateTime until = nextExmEvent?.Timestamp ?? DateTime.MaxValue;

                // Get all pages browser from this page event, filtering out
                // the parent PageViewEvent of the current EXM event
                List<PageViewEvent> browsedPages = interaction
                    .Events
                    .OfType<PageViewEvent>()
                    .Where(x => x.Timestamp >= emailEvent.Timestamp && x.Timestamp < until && x.Id != emailEvent.ParentEventId)
                    .ToList();

                // Get the parent PageViewEvent of the current EXM event
                PageViewEvent parentEvent = interaction
                    .Events
                    .OfType<PageViewEvent>()
                    .SingleOrDefault(x => x.Id == emailEvent.ParentEventId);

                // Get any goals related to the current EXM event
                List<Goal> goals = interaction
                    .Events
                    .OfType<Goal>()
                    .Where(x => x.Timestamp >= emailEvent.Timestamp && x.Timestamp < until)
                    .ToList();

                // Set the number of page view for the current EXM event
                // If no pages have been browsed to, this number will be 1
                dimension.MetricsValue.PageViews += browsedPages.Count;

                // If the contact clicked any pages on the landing page, 
                // this is not a bounce
                dimension.MetricsValue.Bounces = browsedPages.Count > 0 ? 0 : 1;

                // Calculate and set the engagement value
                dimension.MetricsValue.Value = interaction.Events.Where(x => x.Timestamp >= emailEvent.Timestamp && x.Timestamp < until).Sum(x => x.EngagementValue);

                // Set the time spent on the site
                dimension.MetricsValue.TimeOnSite += (int)(parentEvent?.Duration.TotalSeconds ?? 0);

                // Set the number of conversions i.e. number of goals hit
                dimension.MetricsValue.Conversions = goals.Count;

                // If this is the first click/open for this specific message-contact combination, 
                // increment Count by 1, specifying that this is a unique interaction
                if ((emailEventType == EmailEventType.Click || emailEventType == EmailEventType.Open) &&
                    IsUniqueEvent(interaction.Contact.Id, emailEvent, emailEventType))
                {
                    dimension.MetricsValue.Count = 1;
                }

                yield return dimension;
            }
        }

        /// <summary>
        /// Generates the custom part of dimension's key.
        /// </summary>
        /// <param name="interaction">The <see cref="Interaction"/>.</param>
        /// <param name="exmEvent">The <see cref="EmailEvent"/>.</param>
        /// <param name="eventType">The <see cref="EmailEventType"/></param>
        internal abstract string GenerateCustomKey([NotNull] Interaction interaction, [NotNull] EmailEvent exmEvent, EmailEventType eventType);

        /// <summary>
        /// Checks if the event is the first occurence of the event or not, for a specific message-contact combination.
        /// An in-memory cache is checked first, and if the message-contact combination is not found in the cache,
        /// the calculated contact facet <see cref="ExmKeyBehaviorCache"/> is loaded from xConnect. If the event is found 
        /// in the facet, the in-memory cache is updated.
        /// </summary>
        /// <param name="contactId">The contact id</param>
        /// <param name="emailEvent">The email event</param>
        /// <param name="emailEventType">The type of the event</param>
        /// <returns><c>true</c> if this is the first occurence of the event. <c>false</c> if not</returns>
        private bool IsUniqueEvent(Guid? contactId, EmailEvent emailEvent, EmailEventType emailEventType)
        {
            if (!contactId.HasValue)
            {
                return false;
            }

            if (_uniqueEventCache.HasUniqueEvent(contactId.Value, emailEvent.MessageId, emailEvent.InstanceId, emailEventType, emailEvent.Id))
            {
                return true;
            }

            Contact contact = GetContact(contactId);

            ExmKeyBehaviorCache exmKeyBehaviorCache = contact?.ExmKeyBehaviorCache();
            
            if (exmKeyBehaviorCache?.UniqueEvents == null)
            {
                return false;
            }

            string key = exmKeyBehaviorCache.GetUniqueEventDictionaryKey(emailEvent.MessageId, emailEvent.InstanceId, emailEventType);

            Guid eventId = new Guid();

            if (!exmKeyBehaviorCache.UniqueEvents.TryGetValue(key, out eventId))
            {
                return false;
            }

            bool isUniqueEvent = emailEvent.Id == eventId;

            if (isUniqueEvent)
            {
                _uniqueEventCache.SetUniqueEvent(contactId.Value, emailEvent.MessageId, emailEvent.InstanceId, emailEventType, emailEvent.Id);
            }

            return isUniqueEvent;

        }

        /// <summary>
        /// Loads the xDB contact and the <see cref="ExmKeyBehaviorCache"/> calculated facet, associated with the interaction
        /// </summary>
        /// <param name="contactId">The contact id</param>
        /// <returns>The <see cref="Contact"/></returns>
        private Contact GetContact(Guid? contactId)
        {
            if (!contactId.HasValue)
            {
                return null;
            }

            var reference = new ContactReference(contactId.Value);
            Contact contact = null;
            _xConnectRetry.RequestWithRetry(client =>
            {
                contact = client.Get(reference, new ContactExpandOptions(ExmKeyBehaviorCache.DefaultFacetKey));
            });
            return contact;
        }

        private string CreateDimensionKey(Interaction interaction, EmailEvent emailEvent, EmailEventType emailEventType)
        {
            string customKey = GenerateCustomKey(interaction, emailEvent, emailEventType);

            return customKey == null ? null : string.Format(CultureInfo.InvariantCulture, "{0}_{1}", GenerateBaseKey(emailEvent.ManagerRootId, emailEvent.MessageId), customKey);
        }

        /// <summary>
        /// Generates the base part of dimension's key.
        /// </summary>
        /// <param name="managerRootId">The manager root id</param>
        /// <param name="messageId">The manager root id</param>
        private string GenerateBaseKey(Guid managerRootId, Guid messageId)
        {
            if (managerRootId == default(Guid))
            {
                Logger.LogDebug(string.Format(CultureInfo.InvariantCulture, Sitecore.Support.EmailCampaign.ExperienceAnalytics.Properties.Settings.Default.VisitAggregationStateParameterIsNullOrEmptyMessagePattern,
                    "ManagerRootId", GetType().Name));
                return null;
            }

            if (messageId == default(Guid))
            {
                Logger.LogDebug(string.Format(CultureInfo.InvariantCulture, Sitecore.Support.EmailCampaign.ExperienceAnalytics.Properties.Settings.Default.VisitAggregationStateParameterIsNullOrEmptyMessagePattern,
                    "MessageId", GetType().Name));
                return null;
            }

            return
                new HierarchicalKeyBuilder()
                    .Add(managerRootId)
                    .Add(messageId)
                    .ToString();
        }
    }
}
