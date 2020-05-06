using Sitecore.EmailCampaign.ExperienceAnalytics.Properties;
using System;
using System.CodeDom.Compiler;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;


namespace Sitecore.Support.EmailCampaign.ExperienceAnalytics.Properties
{
    [CompilerGenerated]
    [GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.3.0.0")]
    internal sealed class Settings : ApplicationSettingsBase
    {
        private static Settings defaultInstance = (Settings)SettingsBase.Synchronized(new Settings());

        public static Settings Default => defaultInstance;

        [ApplicationScopedSetting]
        [SettingsDescription("A message pattern for logging a debug message that a dimension will not be processed because parameters are null or empty: {0} - parameter name; {1} - dimension name.")]
        [DebuggerNonUserCode]
        [DefaultSettingValue("Parameter '{0}' of VisitAggregationState is null or empty and the '{1}' dimension will not be processed!")]
        public string VisitAggregationStateParameterIsNullOrEmptyMessagePattern
        {
            get
            {
                return (string)this["VisitAggregationStateParameterIsNullOrEmptyMessagePattern"];
            }
        }

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("0ec316ba-73e7-4c72-9c7d-43a711c11bc9")]
        public Guid ByAbTestVariantSegmentId
        {
            get
            {
                return (Guid)this["ByAbTestVariantSegmentId"];
            }
        }

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("ExmDimensionBase: An exception occurred when getting the Group Resolver value!")]
        public string GetGroupResolverValueExceptionMessage
        {
            get
            {
                return (string)this["GetGroupResolverValueExceptionMessage"];
            }
        }

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("~/icon/office/16x16/mail_open2.png")]
        public string OpenEventImagePath
        {
            get
            {
                return (string)this["OpenEventImagePath"];
            }
        }

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("~/icon/office/16x16/mouse_pointer.png")]
        public string ClickEventImagePath
        {
            get
            {
                return (string)this["ClickEventImagePath"];
            }
        }

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("~/icon/office/16x16/mail_exchange.png")]
        public string BounceEventImagePath
        {
            get
            {
                return (string)this["BounceEventImagePath"];
            }
        }

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("~/icon/office/16x16/mail_bug.png")]
        public string SpamEventImagePath
        {
            get
            {
                return (string)this["SpamEventImagePath"];
            }
        }

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("unspecified")]
        public string UnspecifiedValue
        {
            get
            {
                return (string)this["UnspecifiedValue"];
            }
        }

        [ApplicationScopedSetting]
        [SettingsDescription("A message pattern for logging a debug message that a dimension will not be processed because the facet is not available: {0} - facet name; {1} - dimension name.")]
        [DebuggerNonUserCode]
        [DefaultSettingValue("The '{0}' facet is not available and the '{1}' dimension will not be processed!")]
        public string FacetIsNotAvailableMessagePattern
        {
            get
            {
                return (string)this["FacetIsNotAvailableMessagePattern"];
            }
        }

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("c1745f34-f2b9-4ac3-a6de-faee8ce62ae1")]
        public Guid ByLandingPageSegmentId
        {
            get
            {
                return (Guid)this["ByLandingPageSegmentId"];
            }
        }

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("399d686d-16b6-46e3-89e9-44fb9535c2b2")]
        public Guid ByTimeOfDaySegmentId
        {
            get
            {
                return (Guid)this["ByTimeOfDaySegmentId"];
            }
        }

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("~/icon/office/16x16/window_close.png")]
        public string DispatchFailedEventImagePath
        {
            get
            {
                return (string)this["DispatchFailedEventImagePath"];
            }
        }

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("7558fc89-c25f-4606-bbc5-43b91a382ac9")]
        public Guid ByMessageSegmentId
        {
            get
            {
                return (Guid)this["ByMessageSegmentId"];
            }
        }
    }
}