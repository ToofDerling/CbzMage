# CbzMage.
CzbMage is a comic book converter. It aims to do exactly two things:
1. **Convert azw files to nice cbz files**, ready to read in your favorite cbz reader. Azw files entirely like the ones found in Kdl for PC or Mac. 
Additionally, if CbzMage finds a matching azw.res file it will **merge in any HD images found for the highest possible quality**. 
Comic title and publisher will be read from the azw file, and running CbzMage in scan mode will allow you to edit the values before the conversion. You can also point CbzMage at a file or directory and it will process any azw or azw3 files it finds directly.
2. **Convert pdf files to nice cbz files**. Point CbzMage at a single pdf comic book or a directory of pdf comic books and it will convert them to cbz files in the highest possible quality.

**All of this works fully in [CbzMage Version 0.27](https://github.com/ToofDerling/CbzMage/releases/tag/v0.27).**

CbzMage is a commandline tool written in c#. It requires no installation, very little configuration and no drm plugin/tool (the last part may change in the future). It does require that **[.NET 6](https://dotnet.microsoft.com/en-us/download)** is installed on your computer. The pdf conversion requires that **[Ghostscript version 10+](https://ghostscript.com/releases/gsdnld.html)** is installed on your computer.

CbzMage is released for Windows, Linux, and macOS (but support for the macOS version will be limited as I don't own a Mac). Since the Kdl app only works on PC and Mac the azw conversion is probably not relevant for Linux users, but the pdf conversion works as advertised (but see the note about Ghostscript 10 on Linux below).

Download CbzMage to your hard drive and unpack it anywhere. Have a look at the settings in AzwConvertSettings.json or PdfConvertSettings.json, they are all thoroughly documented there (I hope). Open a command shell and run CbzMage right away, or check out some more information: 

## Azw conversion.

Open the AzwSettings.json file in a text editor and **configure AzwDir**. Please note the comment about [moving the azw directory](https://github.com/ToofDerling/CbzMage/wiki/How-to-move-Kdl-content-folder.) as **running CbzMage for the first time may double the size of your azw directory.**

**Important:** Close Kdl before running CbzMage. Kdl locks some of the azw files when it's running, so there's a high chance that CbzMage will crash because it can't read a locked azw file.

**Running CbzMage with the "AzwConvert" parameter.**

* In the titles directory (TitlesDir in AzwSettings.json) you will find a small file with the title and publisher of each comic book currently in the azw directory.  
* In a subdirectory of the titles directory you will find a similar file for each converted title. If you ever want **to reconvert a title simply delete the title file from the converted titles directory.**
* In the cbz directory (CbzDir in AzwSettings.json) you will find the converted comic books sorted by publisher. 
* If you set SaveCover to true in AzwSettings.json CbzMage will save a copy of the cover image together with the cbz file. If you specify SaveCoverDir the cover image will be saved there instead. There's even a SaveCoverOnly option if you just want the covers.

**Running CbzMage with the "AzwScan" parameter.**

* Like the conversion, in the titles directory you will find a small file with the title and publisher of each comic book currently in the azw directory.  
* Unlike the conversion, each new title will have a ".NEW" marker (and each upgraded title will have an ".UPDATED" marker). 
* You can now edit the publishers and titles as you like and the values will be used when you run AzwConvert. You don't have to remove the markers, the conversion will handle it automatically.

**Azw conversion notes.**

* **Updated title.** This means a title that has been upgraded, ie it now has a HD cover or more HD images than before. CbzMage will scan the azw files and add the ".UPDATED" marker to any upgraded title. It will also detect if a title has been downgraded, this is of course not supposed to happen. Upgrades happen seldom and I have never seen a title being downgraded. 
* **The titles directory** will always reflect the comic books currently in Kdl and is updated each time you run CbzMage. If you edit publisher or name of a title the values will be used when the title is converted 
* **The converted titles directory** contains your converted titles. To reconvert a title you must delete the file in that directory. 
* **The database.** In the titles directory there's a database file with the state of every title that has passed through CbzMage. It's used when checking if a title has been updated and to store the new name and publisher of the title if you edit these values.

**Using CbzMage to convert or scan files directly.**

* **Specify a file or directory after the AzwConvert or AzwScan command** and CbzMage will process any azw or azw3 files it finds directly. All the TitleDir infrastructure is ignored. 
* Add two asterisks after the directory to search subdirectories for files. On Windows: [directory]\\** On Linux (and macOs, I think) it must be included in quotes: "[directory]/\**"
* You can still use CbzDir to tell CbzMage where to create the cbz files, else they will be created in the directory with the azw/azw3 files.
* SaveCover, SaveCoverDir, and SaveCoverOnly also works.

## Pdf conversion.

It's been mentioned before, but let me say it again: pdf conversion requires that that **[Ghostscript version 10+](https://ghostscript.com/releases/gsdnld.html)** is installed on your computer. Once you have that part working there's no need to configure anything, simply try

**Running CbzMage with the "PdfConvert" parameter.**

* Open a command shell and point CbzMage at a single pdf file or a directory with pdf files and it will happily create a cbz file alongside each pdf (unless you have configured CbzDir in PdfConvertSettings.json, then they will created in that directory). That's all there is to it, really.

**Pdf conversion notes.**

* **Ghostscript 10 on Linux.** The only distro I know of that has upgraded to Ghostscript version 10 is [Arch Linux](https://archlinux.org/). I tried a handful of the popular ones and they were all at version 9, which doesn't work with CbzMage. On distros other than Arch you can use the [snap build of Ghostscript 10](https://ghostscript.com/releases/gsdnld.html) which worked fine when I tested it on openSUSE Tumbleweed (the regular Ghostscript build found on the same page was a bit flaky during tests).
* **Search subdirectories for pdf files**. To do this add two asterisks after the directory. On Windows: [directory]\\** On Linux (and macOs, I think) it must be included in quotes: "[directory]/\**"
* **Cbz filesize.** Cbz files created by PdfConvert will typically be 50 - 100 % larger than the original pdf file. Now and then they're smaller and sometimes much larger - though CbzMage tries to handle the most extreme cases without sacrificing any of the conversion quality (see the MinimumHeight and MaximumHeight settings in PdfConvertSettings.json).
* **SaveCover and CbzDir** both works the same as for AzwConvert. And the same goes for the rest of the settings that are shared between the two conversion modes.

##

**Notes.**

* **The "GUI" mode.** (Windows only). This simply means that if you run CbzMage by doubleclicking the exe it will detect that and make the window hang around until you press enter. You can also create a shortcut for each of the modes (open Properties for the shortcut and add the parameter in Target) and have one file to doubleclick for azw conversion and one for converting pdfs in a specific directory.      

**Credits.**

The [azw parser](https://github.com/ToofDerling/MobiMetadata) is mostly copied from the [Mobi Metadata Reader](https://www.mobileread.com/forums/showthread.php?t=185565) by Limey. I cleaned it up, fixed the FullName parsing, and added support for retrieving SD and HD images, the rest is Limey's work. This [Stack Overflow post](https://stackoverflow.com/questions/24233834/getting-cover-image-from-a-mobi-file) was very helpful when figuring out how to extract cover images. Azw6 header structure was gleaned from [UnpackKindleS](https://github.com/Aeroblast/UnpackKindleS)

**That's it.** 

If you have any questions or want to request a feature use [Discussions](https://github.com/ToofDerling/CbzMage/discussions). If you want to report a bug use [Issues](https://github.com/ToofDerling/CbzMage/issues). Happy converting.
