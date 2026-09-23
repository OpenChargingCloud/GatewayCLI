/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of GatewayCLI <https://github.com/OpenChargingCloud/GatewayCLI>
 *
 * Licensed under the Affero GPL license, Version 3.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.gnu.org/licenses/agpl.html
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using System.Reflection;

using org.GraphDefined.Vanaheimr.CLI;

#endregion

namespace cloud.charging.open.Gateway.CommandLine
{

    /// <summary>
    /// The command line of a running gateway.
    /// </summary>
    /// <remarks>
    /// Everything a command needs is reachable from here, which is why every
    /// command takes one of these: the gateway itself, and through it its
    /// configuration, its log and everything the JSON API can do. A command is
    /// a third way of asking for the same thing, beside the web interface and
    /// the switches at a start - never an implementation of its own.
    ///
    /// Commands are not listed anywhere. The constructor asks Styx to walk this
    /// assembly for anything that implements ICLICommand and can be built from
    /// a GatewayCLI, so a new command is a new file and nothing else.
    /// </remarks>
    public class GatewayCLI : CLI
    {

        #region Properties

        /// <summary>
        /// The gateway these commands are about.
        /// </summary>
        public Gateway Gateway { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create the command line of the given gateway.
        /// </summary>
        /// <param name="Gateway">The running gateway.</param>
        /// <param name="AssembliesWithCLICommands">Further assemblies to search for commands. This one is searched either way.</param>
        public GatewayCLI(Gateway            Gateway,
                          params Assembly[]  AssembliesWithCLICommands)

            : base(AssembliesWithCLICommands)

        {

            this.Gateway = Gateway;

            RegisterCLIType(typeof(GatewayCLI));

        }

        #endregion


        #region (protected override) GetPrompt()

        /// <summary>
        /// Which port this gateway answers on, because a machine that is one of
        /// several on a bench should say which one it is before it asks for a
        /// command.
        /// </summary>
        protected override String GetPrompt()

            => $"gateway:{Gateway.HTTPPort}> ";

        #endregion

    }

}
