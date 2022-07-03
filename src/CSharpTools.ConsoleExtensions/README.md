# CSharpTools.ConsoleExtensions
This library contains classes that add extra tools for the console such as progress bars and coloured loggers.  

## Installation:
Grab the latest build from the releases folder [here](./bin/Release/netstandard2.0/CSharpTools.ConsoleExtensions.dll).  
Once you have done that, add the dll to your projects refrence files.

## Examples:
### ProgressBar:
```cs
ConsoleTools.ProgressBar progressBar = new ConsoleTools.ProgressBar("<PREFIX>");
progressBar.setProgress(25);
//Output: <PREFIX> [===>    ] 25%
//The width of the bar depnds on the size of the console and the length of the prefix.
```