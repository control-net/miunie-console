﻿// This file is part of Miunie.
//
//  Miunie is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  Miunie is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with Miunie. If not, see <https://www.gnu.org/licenses/>.

using Microsoft.Extensions.DependencyInjection;
using Miunie.ConsoleApp.Arguments;
using Miunie.ConsoleApp.Configuration;
using Miunie.Core;
using Miunie.Core.Entities;
using System;
using System.Threading.Tasks;

namespace Miunie.ConsoleApp
{
    internal static class Program
    {
        private static MiunieBot _miunie;
        private static ConfigManager _configManager;
        private static ConfigurationFileEditor _editor;
        private static ServerMenu _serverMenu;
        private static RuntimeArguments _args;

        public static void DisplayMenu()
        {
            Console.Clear();
            PrintHeader();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(ConsoleStrings.MAIN_MENU_OPTIONS);
        }

        public static void ClearCurrentConsoleLine()
        {
            var currentTop = Console.CursorTop;
            var currentLeft = Console.CursorLeft;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(currentLeft, currentTop);
        }

        private static async Task Main(string[] args)
        {
            Console.Title = ConsoleStrings.BOT_NAME;
            _miunie = ActivatorUtilities.CreateInstance<MiunieBot>(InversionOfControl.Provider);

            _args = ArgumentsParser.Parse(args);

            if (_args.Headless) { await RunHeadless(); }

            _serverMenu = new ServerMenu(_miunie);

            _configManager = InversionOfControl.Provider.GetRequiredService<ConfigManager>();
            _editor = InversionOfControl.Provider.GetRequiredService<ConfigurationFileEditor>();
            _miunie.MiunieDiscord.ConnectionChanged += MiunieOnConnectionStateChanged;
            await HandleInput();
        }

        private static async Task RunHeadless()
        {
            if (!_args.HasToken)
            {
                Console.WriteLine(ConsoleStrings.HEADLESS_REQUIRES_TOKEN);
                Environment.Exit(0);
            }

            _miunie.BotConfiguration.DiscordToken = _args.Token;
            await _miunie.StartAsync();
        }

        private static void MiunieOnConnectionStateChanged(object sender, EventArgs e)
            => DrawMiunieState();

        private static async Task HandleInput()
        {
            while (true)
            {
                DisplayMenu();
                DrawMiunieState();
                Console.Write(ConsoleStrings.PLEASE_ENTER_MENU_NUMBER);
                var success = int.TryParse(Console.ReadLine(), out var choice);

                if (!success)
                {
                    Console.WriteLine(ConsoleStrings.ERROR_CHOICE_NOT_A_NUMBER);
                }

                switch (choice)
                {
                    case 1:
                        {
                            string token;
                            do
                            {
                                Console.Clear();
                                Console.Write(ConsoleStrings.ENTER_TOKEN);
                                token = Console.ReadLine();

                                Console.Clear();
                                Console.WriteLine(ConsoleStrings.IS_TOKEN_CORRECT, token);
                                Console.WriteLine(ConsoleStrings.YES_NO_PROMPT);
                            }
                            while (Console.ReadKey().Key != ConsoleKey.Y);

                            _editor.WriteSetting(ConsoleStrings.DISCORD_TOKEN, token);
                            _editor.Save();
                            _miunie.BotConfiguration.DiscordToken = token;
                            break;
                        }

                    case 2:
                        {
                            if (_miunie.MiunieDiscord.ConnectionState == ConnectionState.CONNECTED)
                            {
                                _miunie.Stop();
                                AnyKeyToContinue();
                            }
                            else if (_miunie.MiunieDiscord.ConnectionState == ConnectionState.DISCONNECTED)
                            {
                                _miunie.BotConfiguration.DiscordToken = _configManager.GetValueFor(ConsoleStrings.DISCORD_TOKEN);
                                _miunie.BotConfiguration.CommandsEnabled = true;
                                _ = _miunie.StartAsync();
                                AnyKeyToContinue();
                            }

                            break;
                        }

                    case 3:
                        {
                            Environment.Exit(0);
                            break;
                        }

                    case 4:
                        {
                            if (_miunie.MiunieDiscord.ConnectionState == ConnectionState.CONNECTED)
                            {
                                await _serverMenu.ServerMenuAsync();
                            }
                            else
                            {
                                Console.WriteLine(ConsoleStrings.BOT_IS_NOT_RUNNING);
                                AnyKeyToContinue();
                            }

                            break;
                        }

                    default:
                        {
                            Console.WriteLine(ConsoleStrings.UNKNOWN_OPTION_SELECTED);
                            AnyKeyToContinue();
                            break;
                        }
                }
            }
        }

        private static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(ConsoleStrings.MIUNIE_ASCII_HEADER);
            Console.ResetColor();
        }

        private static void AnyKeyToContinue()
        {
            Console.WriteLine(ConsoleStrings.ANY_KEY_TO_CONTINUE);
            _ = Console.ReadKey(true);
        }

        private static void DrawMiunieState()
        {
            var prevCursorLeft = Console.CursorLeft;
            var prevCursorTop = Console.CursorTop;

            var msg = _miunie.MiunieDiscord.ConnectionState == ConnectionState.CONNECTED
                ? ConsoleStrings.BOT_IS_RUNNING
                : ConsoleStrings.BOT_IS_NOT_RUNNING;

            var left = Math.Clamp(Console.WindowWidth - msg.Length, 0, Console.WindowWidth);

            Console.SetCursorPosition(left, 0);
            ClearCurrentConsoleLine();
            Console.Write(msg);
            Console.SetCursorPosition(prevCursorLeft, prevCursorTop);
        }
    }
}
