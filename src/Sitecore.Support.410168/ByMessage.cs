// © 2016 Sitecore Corporation A/S. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Sitecore.EmailCampaign.ExperienceAnalytics.Properties;
using Sitecore.EmailCampaign.Model;
using Sitecore.EmailCampaign.Model.XConnect.Events;
using Sitecore.EmailCampaign.XConnect.Web;
using Sitecore.ExM.Framework.Diagnostics;
using Sitecore.XConnect;
using Sitecore.XConnect.Collection.Model;
using Sitecore.EmailCampaign.ExperienceAnalytics.Dimensions;
using Sitecore.EmailCampaign.ExperienceAnalytics;

namespace Sitecore.Support.EmailCampaign.ExperienceAnalytics.Dimensions
{
    /// <summary>
    /// Implementation of dimension by message.
    /// </summary>
    internal class ByMessage : Sitecore.Support.EmailCampaign.ExperienceAnalytics.Dimensions.ExmDimensionBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ByMessage"/> class.
        /// </summary>
        /// <param name="dimensionId">Dimension's ID.</param>
        public ByMessage(Guid dimensionId)
            : base(dimensionId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ByMessage"/> class.
        /// </summary>
        /// <param name="logger">Logger for current dimension.</param>
        /// <param name="uniqueEventCache">The cache for unique events</param>
        /// <param name="xConnectRetry">The xConnect operation helper</param>
        /// <param name="dimensionId">Dimension's ID.</param>
        internal ByMessage(ILogger logger, IUniqueEventCache uniqueEventCache, XConnectRetry xConnectRetry, Guid dimensionId)
            : base(logger, uniqueEventCache, xConnectRetry, dimensionId)
        {
        }

        /// <inheritdoc />
        internal override string GenerateCustomKey(Interaction interaction, EmailEvent exmEvent, EmailEventType eventType)
        {
            if (exmEvent.MessageLanguage == null)
            {
                Logger.LogDebug(string.Format(CultureInfo.InvariantCulture, Sitecore.Support.EmailCampaign.ExperienceAnalytics.Properties.Settings.Default.VisitAggregationStateParameterIsNullOrEmptyMessagePattern, "MessageLanguage", GetType().Name));
                exmEvent.MessageLanguage = "en";
            }

            var isProductive = false;
            var isBrowsed = false;

            IEnumerable<EmailEvent> exmEvents = interaction.Events.OfType<EmailEvent>();
            EmailEvent nextExmEvent = exmEvents.FirstOrDefault(x => x.Timestamp > exmEvent.Timestamp);
            DateTime until = nextExmEvent?.Timestamp ?? DateTime.MaxValue;
            int browsedPages = interaction.Events.OfType<PageViewEvent>().Count(x => x.Timestamp >= exmEvent.Timestamp && x.Timestamp < until);
            int engagementValue = interaction
                .Events
                .Where(x => x.Timestamp >= exmEvent.Timestamp && x.Timestamp < until)
                .Sum(x => x.EngagementValue);

            WebVisit webVisit = interaction.WebVisit();
            if (webVisit != null)
            {
                isProductive = engagementValue > 0;
                isBrowsed = browsedPages > 1;
            }

            return new KeyBuilder()
                .Add(((int)eventType).ToString(CultureInfo.InvariantCulture))
                .Add(exmEvent.MessageLanguage)
                .Add(isProductive)
                .Add(isBrowsed)
                .ToString();
        }
    }
}
