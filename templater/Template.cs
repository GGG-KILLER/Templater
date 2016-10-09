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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BConsoleFramework;
using GUtils.IO;
using Newtonsoft.Json;

namespace templater
{
    public delegate void FileProcessed ( String FileName, Int32 Processed, Int32 Total );
    public delegate void TaskRunning ( String Task );
    public delegate void TaskInterrupted ( String Task, Exception Ex );

    internal class Template
    {
        /// <summary>
        /// Name of the template
        /// </summary>
        public String Name { get; private set; }

        /// <summary>
        /// The path of the template
        /// </summary>
        public String TPath { get; private set; }

        /// <summary>
        /// The configuration object of the template
        /// </summary>
        public TemplateConfig TConfig { get; private set; }

        /// <summary>
        /// Triggered at the end of a file copy
        /// </summary>
        public event FileCopied FileCopied;

        /// <summary>
        /// Triggered at the end of a file processing
        /// </summary>
        public event FileProcessed FileProcessed;

        /// <summary>
        /// Event triggered when a task started running
        /// </summary>
        public event TaskRunning TaskRunning;

        /// <summary>
        /// Event triggered when a task was interrupted by reaching timeout
        /// </summary>
        public event TaskInterrupted TaskInterrupted;

        /// <summary>
        /// Instantiates a template
        /// </summary>
        /// <param name="TemplateName">The template name(folder name)</param>
        public Template ( String TemplateName )
        {
            // Gets the path of the template
            var path = Path.Combine (
                AppDomain.CurrentDomain.BaseDirectory,
                "templates",
                TemplateName
            );

            // Checks if the directory exists
            if ( !Directory.Exists ( path ) )
                throw new DirectoryNotFoundException ( $"The template directory ({path}) was not found." );

            // Sets the data of the template
            this.Name = TemplateName;
            this.TPath = path;

            // Loads the template configuration
            var configPath = Path.Combine ( path, "template.json" );
            if ( !File.Exists ( configPath ) )
                throw new Exception ( "Invalid template. Missing \"template.json\" configuration file." );

            // Parses the json
            try
            {
                using ( var reader = File.OpenText ( configPath ) )
                    this.TConfig = JsonConvert.DeserializeObject<TemplateConfig> ( reader.ReadToEnd ( ) );

                this.TConfig.tasksTimeout = Math.Max ( TConfig.tasksTimeout, 60000 );
            }
            catch ( Exception )
            {
                this.TConfig = new TemplateConfig ( );
            }
        }

        /// <summary>
        /// Implements the template on the provided path
        /// </summary>
        /// <param name="ImplementationPath">The path to implement the template on</param>
        /// <param name="Data">Data to replace on the files while preprocessing</param>
        /// <returns>void</returns>
        public async Task ImplementAsync ( String ImplementationPath, IDictionary<String, String> Data )
        {
            ImplementationPath = Path.GetFullPath ( ImplementationPath );
            // Checks if the implementation folder exists
            Directory.CreateDirectory ( ImplementationPath );

            // List all files and ignores the ones that should be
            var files = GetFiles ( TPath, TConfig.ignore
                .Concat ( new String[1] { "template.json" } ) );

            // Copies all files to their targets
            var copier = new FileCopier ( );
            copier.FileCopied += Copier_FileCopied;
            await copier.CopyFilesAsync (
                files.Select ( file => Path.Combine ( TPath, file ) ),
                files.Select ( file => Path.Combine ( ImplementationPath, file ) )
            );

            // List files to process
            var processable = GetFiles ( ImplementationPath, TConfig.ignore
                .Concat ( TConfig.processIgnore )
                .Concat ( new String[1] { "template.json" } ) );

            // Processes the files
            for ( var i = 0 ; i < processable.Length ; i++ )
            {
                ProcessFile ( Path.Combine ( ImplementationPath, processable[i] ), Data );
                FileProcessed?.Invoke ( processable[i], i, processable.Length );
            }

            foreach ( var command in TConfig.setupTasks )
            {
                TaskRunning?.Invoke ( command );
                Process task = null;
                try
                {
                    task = Process.Start ( new ProcessStartInfo
                    {
                        FileName = command.Before ( ' ' ),
                        Arguments = command.After ( ' ' ),
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = false,
                        UseShellExecute = true,
                        LoadUserProfile = true,
                        WorkingDirectory = ImplementationPath
                    } );
                    // Runs the process for 5 minutes in max

                    task.WaitForExit ( TConfig.tasksTimeout );

                    if ( task.ExitCode != 0 )
                        throw new Exception ( $"Exit code was not 0: {task.ExitCode}." );

                    var elapsed = task.ExitTime - task.StartTime;
                    if ( elapsed.TotalMilliseconds >= TConfig.tasksTimeout - 2 )
                        throw new Exception ( $"Process achieved the timeout: {elapsed.ToString ( )}" );
                }
                catch ( Exception ex )
                {
                    TaskInterrupted?.Invoke ( command, ex );
                }
                finally
                {
                    task.Close ( );
                    task.Dispose ( );
                }
            }
        }

        /// <summary>
        /// Redirects the file copied event
        /// </summary>
        /// <param name="FileName">Name of the file that was copied</param>
        /// <param name="Processed">The amount of processed files</param>
        /// <param name="Total">The total files to process</param>
        private void Copier_FileCopied ( String FileName, Int32 Processed, Int32 Total )
        {
            FileCopied?.Invoke ( FileName, Processed, Total );
        }

        /// <summary>
        /// Processes the file and replaces all
        /// </summary>
        /// <param name="Path">The path of the file to process</param>
        /// <param name="Replacements">The replacement parameters</param>
        private static void ProcessFile ( String Path, IDictionary<String, String> Replacements )
        {
            var lines = File.ReadAllLines ( Path );
            lines = lines
                .Select ( line => ReplaceDict ( line, Replacements ) )
                .ToArray ( );
            File.WriteAllLines ( Path, lines );
        }

        private String[] GetFiles ( String Path, IEnumerable<String> IgnorePatterns = null )
        {
            return new GlobSearch ( new String[] { "**/*" }, IgnorePatterns )
                .Search ( TPath )
                .ToArray ( );
        }

        /// <summary>
        /// Replaces values on a string with an object
        /// </summary>
        /// <param name="raw">The raw string</param>
        /// <param name="replacements">The object to use on the replacing</param>
        /// <returns></returns>
        private static String ReplaceDict ( String raw, IDictionary<String, String> replacements = null )
        {
            var res = new StringBuilder ( raw );
            if ( replacements != null )
                foreach ( var kv in replacements )
                    res.Replace ( $"{{{kv.Key}}}", kv.Value );
            return res.ToString ( );
        }
    }

    internal class TemplateConfig
    {
        public String[] setupTasks = new String[0],
            ignore = new String[0],
            processIgnore = new String[0];

        public Int32 tasksTimeout = 60000; // 1 min
    }
}