/*
Copyright 2016 Martin Haesemeyer

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/
using System;

namespace TwoPAnalyzer.PluginAPI
{
    /// <summary>
    /// Main interface to be implemented by all 2PAnalyzer plugins.
    /// Base interface of type specific interfaces
    /// </summary>
    /// <remarks>Allows identification of plugin classes and integration into the program menu structure</remarks>
    public interface IPlugin
    {
        /// <summary>
        /// The name of the plugin also
        /// serving as the name of its menu item
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Optional to group plugins in the
        /// menu structure
        string SubCategory { get; }

        /// <summary>
        /// The type of the plugin. Valid plugins
        /// need to return their specific interface type
        /// </summary>
        Type PluginType { get; }
    }
}
