# AutoStemReloaded
### Overview
Convert MP3 files to Traktor STEM files.

This is the only standalone, 100% free, opensource tool for converting regular mp3 files into Traktor compatible stem files.
Here you can convert entire music libraries of plain mp3 files into 4 track (drums, bass, vocals & insturmental) stems files for use in DJ software such as Traktor.
Autostem uses the Spleeter or Demucs machine learning algorythim and runs locally on your machine, meaning no annoying subscriptions or paywalls. It's your computer doing the work.

### Features
* Convert mp3 files into Traktor compatible STEM files.
* Convert single files or entire folders of songs.
* Choose between Spleeter and Demucs separation AI algorythims.
* Convert large files.
* Fully configurable STEM options, track colors and names, compressor and limiter.
* Duplicate song finder/manager.
* Convert STEM files back into simple MP3s. (Useful for upgrading music from Spleeter to Demucs quality)

### Getting Started
* Clone or download and extract the repository.
* Navigate to AutoStemReloaded/AutoStemReloadedUnity/Builds these are the program files.
* Move all the contents of this folder into your desired location, the rest of the files can be discarded if you have no interest in the source code.
* Install FFMPEG, Spleeter and/or Demucs
  * https://github.com/deezer/spleeter
  * https://github.com/facebookresearch/demucs
  * https://ffmpeg.org
  * Follow the instructions to install each of those as command line tools.
  * You can test if your installation was successful by opening a command promps window and trying to run ```spleeter```, ```demucs``` and ```ffmpeg```
  * If you get the message ```spleeter/demucs/ffmpeg is not recognized as an internal or external command, operable program or batch file.``` then please verify your installation. Google is your friend.
* If you have an Nvidia GPU then I strongly suggest you install the GPU accelerated version of Demucs. It's much faster than Spleeter (depending on your GPU).
* Run AutoStem.exe
* Enjoy! (Please note: conversions can take a few minutes per song depending on your hardware and other factors)

### Screenshots
![](https://i.ibb.co/mt94wff/autostem5.png)
![](https://i.ibb.co/TvxB4Lj/autostem1.png)
![](https://i.ibb.co/dQ5KXdT/autostem2.png)
![](https://i.ibb.co/t46PvVJ/autostem3.png)
![](https://i.ibb.co/dKHR7Dm/autostem4.png)
