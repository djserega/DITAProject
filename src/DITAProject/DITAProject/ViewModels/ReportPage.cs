﻿using DevExpress.Mvvm;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ITAJira.ViewModels
{
    internal class ReportPage : BindableBase, ISingleton
    {
        private readonly ChartValues<GanttPoint> _values = new();

        public ReportPage()
        {
            MainViewModel.ShowReportEvent += (o, listTasks) => { BuildReport(listTasks); };
            MainViewModel.HideReportEvent += (o, e) => { Series?.Clear(); };
            Views.Reports.ToolTipConverter.FormatterToolTipReport += (int labelKey, string detailed) => 
            {
                if (detailed == "Header")
                    return Labels?[labelKey] ?? string.Empty;
                else if (detailed == "Spent")
                    return GetStringSpentFromKeyLabel(labelKey);
                else
                    return "-";
            };

            BuildReport(new List<Models.JiraModel.Task>());
        }

        public double From { get; set; }
        public double To { get; set; }
        public SeriesCollection? Series { get; set; }
        public Func<double, string> Formatter { get; } = value => value < 0 ? "" : ConvertSpentToString(value);
        public string[]? Labels { get; set; }

        private Dictionary<string, long> _dictAssigneesSpent = new();

        public ICommand ResetZoomCommand { get => new DelegateCommand(() => { ResetZoom(); }); }

        internal void BuildReport(List<Models.JiraModel.Task> tasks)
        {
            if (!tasks.Any())
            {
                tasks = new List<Models.JiraModel.Task>()
                {
                    new()
                    {
                        Key = "ITA-230",
                        Updated = new(2022, 07, 06),
                        AssigneeUser = new(){ Name = "Sergey"},
                        TimeSpentInSeconds = 3600
                    },
                    new()
                    {
                        Key = "ITA-250",
                        Updated = new(2022, 07, 03),
                        AssigneeUser = new(){ Name = "Galyuk"},
                        TimeSpentInSeconds = 14400
                    },
                    new()
                    {
                        Key = "ITA-125",
                        Updated = new(2022, 07, 07),
                        AssigneeUser = new(){ Name = "Galyuk"},
                        TimeSpentInSeconds = 7200
                    },
                    new()
                    {
                        Key = "ITA-50",
                        Updated = new(2022, 07, 01),
                        AssigneeUser = new(){ Name = "Galyuk"},
                        TimeSpentInSeconds = 7261
                    },
                    new()
                    {
                        Key = "ITA-130",
                        Updated = new(2022, 07, 06),
                        AssigneeUser = new(){ Name = "Sergey"},
                        TimeSpentInSeconds = 3600
                    },
                    new()
                    {
                        Key = "ITA-150",
                        Updated = new(2022, 07, 03),
                        AssigneeUser = new(){ Name = "Galyuk"},
                        TimeSpentInSeconds = 14400
                    },
                    new()
                    {
                        Key = "ITA-25",
                        Updated = new(2022, 07, 07),
                        AssigneeUser = new(){ Name = "Galyuk"},
                        TimeSpentInSeconds = 7200
                    },
                    new()
                    {
                        Key = "ITA-550",
                        Updated = new(2022, 07, 01),
                        AssigneeUser = new(){ Name = "Galyuk"},
                        TimeSpentInSeconds = 7261
                    }
            };
            }

            _dictAssigneesSpent.Clear();

            From = default;
            To = default;
            _values.Clear();

            Series?.Clear();
            Labels = Array.Empty<string>();

            IEnumerable<IGrouping<string?, Models.JiraModel.Task>> lastData = tasks.GroupBy(el => el.AssigneeUser?.Name);
            Dictionary<string, long> firstValue = new();

            List<string> listLabels = new();

            foreach (IGrouping<string?, Models.JiraModel.Task> itemGroupName in lastData)
            {
                if (string.IsNullOrEmpty(itemGroupName.Key))
                    continue;

                long spentTime = itemGroupName.Sum(el => el.TimeSpentInSeconds);

                if (firstValue.ContainsKey(itemGroupName.Key))
                    firstValue[itemGroupName.Key] += spentTime;
                else
                    firstValue.Add(itemGroupName.Key, spentTime);

                _values.Add(new GanttPoint(0, spentTime));

                listLabels.Add(itemGroupName.Key);

                _dictAssigneesSpent.Add(itemGroupName.Key, spentTime);
            };

            Labels = listLabels.ToArray();

            Series = new SeriesCollection
            {
                new RowSeries
                {
                    Values = _values,
                    DataLabels = true,
                    LabelPoint = (ChartPoint obj) => { return GetStringSpentFromKeyLabel(obj.Key); }
                }
            };

            ResetZoom();
        }

        private void ResetZoom()
        {
            if (_values.Any())
            {
                From = 0;
                To = _values.Max(el => el.EndPoint);
            }
        }

        private string GetStringSpentFromKeyLabel(int key)
            => ConvertSpentToString(_dictAssigneesSpent[Labels?[key] ?? ""]);

        private static string ConvertSpentToString(double value)
        {
            int min = 0;

            int hours = (int)value / 3600;
            if (hours * 3600 != value)
            {
                min = (int)(value - hours * 3600) / 60;
                if (min < 1)
                    min = 0;
            }

            string convertedValue = $"{(hours == 0 ? "" : $"{(int)hours} ч.")}{(min == 0 ? "" : $" {min} мин.")}";

            return convertedValue;
        }

    }
}
