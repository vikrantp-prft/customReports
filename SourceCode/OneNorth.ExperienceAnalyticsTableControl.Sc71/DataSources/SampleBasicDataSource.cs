﻿using OneNorth.ExperienceAnalyticsTableControl.Sc71.Api;
using System;

namespace OneNorth.ExperienceAnalyticsTableControl.Sc71.DataSources
{
    public class SampleBasicDataSource
    {
        public ExperienceAnalyticsTableControlResponse Get(DateTime dateFrom, DateTime dateTo, string siteName)
        {
            var reportData = new ExperienceAnalyticsTableControlData<dynamic>();

            var random = new Random();

            var count = 10;
            for (var i = 0; i < count; i++)
            {
                var item = new
                {
                    index = i,
                    id = Guid.NewGuid().ToString(),
                    datefrom = dateFrom.ToShortDateString(),
                    dateto = dateTo.ToShortDateString(),
                    sitename = siteName,
                    random = random.Next(0, 1000)
                };

                reportData.AddItem(item);
            }
            var content = new ExperienceAnalyticsTableControlResponse()
            {
                Data = reportData,
                TotalRecordCount = count
            };

            return content;
        }
    }
}
