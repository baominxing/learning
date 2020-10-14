﻿using Flurl;
using Flurl.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlurlHttpDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                //Flurl.Http的使用，NetworkJson转换成JObject对象后操作比较好
                Sample1.Demonstration();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.ReadKey();
        }
    }

    public class Sample1
    {
        public static void Demonstration()
        {
            // var url = "http://ems.handeaxle.com:8093/api/data/depart/now";

            // Task.Run(async () =>
            //{
            //    var result = await url.WithHeader("token", "15002ec8025f4c26b52e9bd47663f221").SetQueryParam("depart_id", "19")
            //    //.SetQueryParams(new { consume_depart = 19, consume_sdate = "2020-09-06", consume_edate = "2020-09-10" })
            //    .GetAsync().ReceiveJson<JObject>();

            //    //今日数据
            //    var now_consume = result.SelectToken("data.now.meter_value.consume")?.Value<double>() ?? 0d;
            //    //昨日数据
            //    var yesterday_value = result.SelectToken("data.yesterday.value")?.Value<double>() ?? 0d;
            //    //去年数据
            //    var lastyear_value = result.SelectToken("data.lastyear.value")?.Value<double>() ?? 0d;
            //    //累计煤标
            //    var today_value = now_consume / 1000;
            //    //昨日同期
            //    var yesterday_rate = now_consume == 0d ? 0d : Convert.ToDouble(yesterday_value) / Convert.ToDouble(now_consume);
            //    //去年同期
            //    var lastyear_rate = now_consume == 0d ? 0d : Convert.ToDouble(lastyear_value) / Convert.ToDouble(now_consume);
            //    //今日数据
            //    var devices = result.SelectTokens("data.now.devices");

            //    var sumPower = new List<double>();
            //    var sumAir = new List<double>();
            //    var sumPowerConsume = new List<double>();
            //    var sumAirConsume = new List<double>();

            //    foreach (var item in devices.Values())
            //    {
            //        var meter_value = item.SelectToken("meter_value");

            //        var code = meter_value?.Value<string>("code") ?? string.Empty;
            //        var value = meter_value?.Value<double>("value") ?? 0d;
            //        var name = meter_value?.Value<string>("name") ?? string.Empty;
            //        var consume = meter_value?.Value<double>("consume") ?? 0d;

            //        if (code == "elec")
            //        {
            //            sumPower.Add(value);
            //        }
            //        else if (code == "compair")
            //        {
            //            sumAir.Add(value);
            //        }

            //        if (name == "电")
            //        {
            //            sumPowerConsume.Add(consume);
            //        }
            //        else if (name == "压缩空气")
            //        {
            //            sumAirConsume.Add(consume);
            //        }
            //    }

            //});

            //var url2 = "http://ems.handeaxle.com:8093/api/data/depart/time";

            //Task.Run(async () =>
            //{
            //    var result = await url2.WithHeader("token", "15002ec8025f4c26b52e9bd47663f221").SetQueryParam("depart_id", "19")
            //    //.SetQueryParams(new { consume_depart = 19, consume_sdate = "2020-09-06", consume_edate = "2020-09-10" })
            //    .GetAsync().ReceiveJson<JObject>();

            //    //今日数据
            //    var data = result.SelectTokens("data");

            //    foreach (var item in data.Values())
            //    {
            //        Console.WriteLine(Math.Round((item.SelectToken("meter_value").Value<double?>("consume") ?? 0d) / 1000, 4));
            //    }

            //});

            var url3 = "http://ems.handeaxle.com:8093/api/consume/data/history";

            Task.Run(async () =>
            {
                var list = new List<IEnumerable<dynamic>>();

                var result = await url3.WithHeader("token", "15002ec8025f4c26b52e9bd47663f221").SetQueryParams(new
                {
                    consume_depart = 19,
                    consume_sdate = DateTime.Now.AddDays(-11).ToString("yyyy-MM-dd"),
                    consume_edate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"),
                }
                )
                .GetAsync().ReceiveJson<JObject>();

                Console.WriteLine(result);

                var datalist = result.SelectTokens("data.list");

                foreach (var item in datalist.Values())
                {
                    Console.WriteLine(item);

                    var gmt_day = item?.Value<DateTime?>("gmt_day")?.ToString("yyyy-MM-dd") ?? "";

                    if (string.IsNullOrEmpty(gmt_day))
                    {
                        continue;
                    }

                    var depart_name = item?.Value<string>("depart_name") ?? "";
                    var value = item?.Value<string>("value") ?? "";
                    var total_cost = item?.Value<string>("total_cost") ?? "";
                    var elec = item?.Value<string>("elec") ?? "";
                    var elec_peak_amount = item?.Value<string>("elec_peak_amount") ?? "";
                    var elec_normal_amount = item?.Value<string>("elec_normal_amount") ?? "";
                    var elec_valley_amount = item?.Value<string>("elec_valley_amount") ?? "";
                    var compair = item?.Value<string>("compair") ?? "";
                    var water = item?.Value<string>("water") ?? "";
                    var gas = item?.Value<string>("gas") ?? "";
                    var rwater = item?.Value<string>("rwater") ?? "";

                    list.Add(new List<dynamic>() { gmt_day, depart_name, value, total_cost, elec, elec_peak_amount, elec_normal_amount, elec_valley_amount, compair, water, gas, rwater });
                }
            });
        }
    }

    internal class Person
    {
    }
}