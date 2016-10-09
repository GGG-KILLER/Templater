# Templater

This is a small console utility that provides you with the ability to use file & directory templates for anything you can imagine.

# What can I do with it?
Want to save a base project and have it available for copy?
You can.

Want the files to have inline values be replaced by console parameters?
You can.

Want to run node.js, composer, npm, gulp or any other program/batch after the files are copied for further processing?
You can.

Want to ignore some of the files on your template (build files, temporary files) inside your template folder?
You can.

Want to have some of the files on your template to not have their inline values replaced?
You also can.

# How does it work?

1. You create a folder with all your template files (and maybe more?);
2. You add a [`template.json` file](https://github.com/GGG-KILLER/Templater#templatejson-configuration) inside your template folder and fill in the fields;
3. You copy your template folder to the `templates` directory in the same directory as the program (create it if it doesn't exists);
4. Now simply call `templater [template folder name] [inline values to replace as -key value pairs]`;
5. The template will be implemented on `<current working directory>/<template folder name>/` automagically.

# Installation
Download the executable from the Releases section and then put it somewhere on your PATH.

# Template.json configuration
The `template.json` is a JSON file (if you don't know what it is, search before trying to create it) and consists of 4 fields (all optional, but the file should exist and be a valid empty json array if you don't use any):

- `setupTasks` - An array of strings containing all commands to run after all files are copied and processed
- `ignore` - An array of strings containing all files to ignore (will not be copied) (Globs supported!)
- `processIgnore` - An array of strings containing all files which won't have inline values replaced. (Globs supported!)
- `tasksTimeout` - Time (in miliseconds) that the program should wait for the tasks to finish before killing them (use -1 to disable. **extremely not recommended, use at your own risk**). Do not use 0 or values smaller than -1 **or the program will break.**

# Inline replacing
The inline replacing (called `processing`) is basically turning this:

`<title>{ProjectName} - Home</title>`

into this:

`<title>Foo Bar - Home</title>`

when the program is run like this:

`templater <templatename> -ProjectName "Foo Bar"`

If you don't want inline values to be replaced in a file, use the `processIgnore` field on the `template.json` file.

# What are Globs?
They are file matching patterns to make your life easier and not have to list all files 1 by 1.
This program supports (as far as I know from Microsoft.Extensions.FileGlobbing, since it doesn't has *any* documentation) 2 patterns:

- \*\* (globstar) - Matches any number of folders at any depth
- \* (star) - Matches any series of characters until a `/`

Example:

- `src/**/*.html` would get all .html files inside src despite how folders deep they were.
- `**` would get all files in whichever directory it searches on.
- `src/*.txt` would get all .txt files inside `src/` but not inside any subfolders.

# License
MIT - https://gggkiller.mit-license.org/

# Requirements

- [GUtils.NET](https://github.com/GGG-KILLER/GUtils.NET) (requires re-referencing)
- [BConsole](https://github.com/GGG-KILLER/GUtils.NET) (requires re-referencing)
- Costura.Fody (nuget restores it automatically)
- Newtonsoft.Json (nuget restores it automatically)
- Microsoft.Extensions.FileGlobbing (nuget restores it automatically)