/*
    Author: Brent Pereira
    Latest Update: May 16th, 2020
    Description: Requests certain Json properties for Discord using DSharpPlus.
*/

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AttendanceBot
{
    public struct ConfigJson
    {
        [JsonProperty("token")]
        public string Token { get; private set; }

        [JsonProperty("prefix")]
        public string Prefix { get; private set; }
    }
}
