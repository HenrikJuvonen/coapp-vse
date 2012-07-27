var filename = "CommonAssemblyInfo.cs";

/// Every time we do a release-sign, we increment the build number.
var rx, major, minor, build, revision;
var fso = new ActiveXObject("Scripting.FileSystemObject");

String.prototype.Trim = function () {
    return (this || "").replace(/^\s+|\s+$/g, "");
};

function SaveTextFile(filename, text) {
    var f = fso.OpenTextFile(filename, 2, true);
    f.Write(text.Trim() + "\r\n");
    f.Close();
}

function LoadTextFile(filename) {
    if (fso.FileExists((filename))) {
        return fso.OpenTextFile(filename, 1, false).ReadAll();
    }
    
    WScript.Echo("Cannot find file: "+ filename);
    return null;
}

/// ---- Developer Tools Pacakge --------------------------------------------------------------------------------------------

if (newTxt = LoadTextFile(filename)) {
    rx = /\[assembly: AssemblyVersion\("(.*)\.(.*)\.(.*)\.(.*)"\)\]/ig.exec(newTxt); // Get Assembly Version
    
    major = parseInt(RegExp.$1.Trim());
    minor = parseInt(RegExp.$2.Trim());
    build = parseInt(RegExp.$3.Trim());
    revision = parseInt(RegExp.$4.Trim())+1;
    
    newTxt = newTxt.replace( /\[assembly: AssemblyVersion.*/ig , '[assembly: AssemblyVersion("'+major+'.'+minor+'.'+build+'.'+revision+'")]' );
    newTxt = newTxt.replace( /\[assembly: AssemblyFileVersion.*/ig , '[assembly: AssemblyFileVersion("'+major+'.'+minor+'.'+build+'.'+revision+'")]' );
   
    WScript.echo('Next version: '+major+'.'+minor+'.'+build+'.'+revision );
    
    WScript.echo("Incrementing Version Attributes in "+filename);
    SaveTextFile(filename, newTxt);
}