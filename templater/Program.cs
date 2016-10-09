/*
 * Copyright © 2016 GGG KILLER <gggkiller2@gmail.com>
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”),
 * to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
 * IN THE SOFTWARE.
 */
using System;
using System.Collections.Generic;
using BConsoleFramework;

namespace templater
{
    internal class Program
    {
        private static readonly Version Version = new Version ( "0.5.0" );

        private static Boolean copyingPrinted, processingPrinted;

        private static void Main ( String[] args )
        {
            Header ( );
            if ( args.Length < 2 || args[0] == "help" )
            {
                HelpText ( );
                return;
            }
            var template = args[0];
            var path = args[1];
            var data = ParseArguments ( args, 2 );

            BConsole.WriteLine ( $"Implementing template {template} on {path}" );
            Template temp;
            try
            {
                temp = new Template ( template );
            }
            catch ( Exception ex )
            {
                BConsole.WriteLine ( $"Error: {ex}", ConsoleColor.Red );
                return;
            }

            temp.FileCopied += Temp_FileCopied;
            temp.FileProcessed += Temp_FileProcessed;
            temp.TaskRunning += Temp_TaskRunning;
            temp.TaskInterrupted += Temp_TaskInterrupted;
            try
            {
                temp.ImplementAsync ( path, data )
                    .Wait ( );
            }
            catch ( Exception ex )
            {
                BConsole.WriteLine ( $"Error: {ex}", ConsoleColor.Red );
                return;
            }
            BConsole.WriteLine ( "Done." );
        }

        private static void Temp_TaskInterrupted ( String Task, Exception Ex )
        {
            BConsole.WriteLine ( $"Task \"{Task}\" errored: {Ex}", ConsoleColor.Red );
        }

        private static void Temp_TaskRunning ( String Task )
        {
            BConsole.WriteLine ( $"Running task \"{Task}\"..." );
        }

        private static void Temp_FileCopied ( String FileName, Int32 Processed, Int32 Total )
        {
            if ( !copyingPrinted )
            {
                BConsole.WriteLine ( "Copying files..." );
                copyingPrinted = true;
            }

            // Turn from array indexing to human counting
            Processed++;
            Total++;
            BConsole.WriteLine ( $"Copied \"{FileName}\"({Processed}/{Total})" );
        }

        private static void Temp_FileProcessed ( String FileName, Int32 Processed, Int32 Total )
        {
            if ( !processingPrinted )
            {
                BConsole.WriteLine ( "Processing files..." );
                processingPrinted = true;
            }
            // Turn from array indexing to human counting
            Processed++;
            Total++;
            BConsole.WriteLine ( $"Processed \"{FileName}\"({Processed}/{Total})" );
        }

        /// <summary>
        /// Prints the program header to the console
        /// </summary>
        private static void Header ( )
        {
            BConsole.WriteLine ( $"Templater v{Version} Copyright GGG KILLER 2016" );
        }

        /// <summary>
        /// Prints the help text to the console
        /// </summary>
        private static void HelpText ( )
        {
            BConsole.WriteLine (
                @"Usage:
    templater template project folder [options...]
    or
    templater help

    template        A template on the templates folder and properly configured

    project folder  A folder that will be created and have the
                    contents moved to

    options...      Any number of options followed by values to be used by
                    the file processor. Example: -Author ""John Doe"" would
                    replace all instances of ""{Author}"" by ""John Doe""
                    in all files that aren't configured to be ignored by the
                    preprocessor.
Template creation:
    Simply put the directory of the template you want to create on the
    ""templates"" directory located on the same folder as this executable and
    then create a ""template.json"" file with at least one of the following keys:
    - ignore            (String array, Optional) Files the program should
                        ignore (won't be copied at all)
    - processIgnore     (String array, Optional) Files the file processor
                        should ignore (won't have values replaced)
    - setupTasks        (String array, Optional) Processes that should be ran
                        after copying and processing all files
    - tasksTimeout      (Integer, Default: 60000) Timeout in miliseconds that
                        the setup tasks should be individually ran for.
                        Use -1 to disable. 0 will break all setup tasks by
                        making them not run"
            );
        }

        /// <summary>
        /// Processes the arguments turning them into a dictionary for
        /// file processing
        /// </summary>
        /// <param name="args">The program arguments</param>
        /// <param name="StartIndex">The index to start processing on</param>
        /// <returns></returns>
        private static IDictionary<String, String> ParseArguments ( String[] args, Int32 StartIndex )
        {
            if ( ( args.Length - StartIndex ) % 2 > 0 )
                throw new Exception ( "The options passed to the program should be even (key and value pairs matching)" );
            var dict = new Dictionary<String, String> ( );

            // Loops through all key value pairs
            for ( var i = StartIndex ; i + 1 < args.Length ; i++ )
            {
                var key = args[i];
                var val = args[++i];

                if ( key[0] != '-' )
                    if ( val[0] == '-' )
                        i--;
                    else
                        throw new Exception ( $@"Invalid parameters passed:
Key {key} does not has a hyphen at the start!" );
                else
                    dict[key.Substring ( 1 )] = val;
            }

            return dict;
        }
    }
}