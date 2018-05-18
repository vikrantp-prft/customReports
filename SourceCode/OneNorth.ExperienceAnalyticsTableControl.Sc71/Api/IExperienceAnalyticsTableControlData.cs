using System.Collections;
using Sitecore.ExperienceAnalytics.Api.Response;

namespace OneNorth.ExperienceAnalyticsTableControl.Sc71.Api
{
    public interface IExperienceAnalyticsTableControlData
    {
        IEnumerable Content { get; }
        Localization Localization { get; set; }
    }
}