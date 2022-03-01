using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Threading;
using System.IO;
using SFB;
using UnityEngine.UI;

public class AutoStemBackup : MonoBehaviour
{
	public RectTransform pivot;
	int pageNumber = 0;
	public bool debugMode;
	[Space]
	public bool highQualityMode;
	string trackPath;
	public string outputPath;
	public bool deleteOriginal;
	[Space]
	public string[] conversionCue;
	[Space]
	public StemData stemData;
	public StemData defaultData;
	[Space]
	string dataPath;
	string tempPath;
	bool pauseState;

	string songsFolder;

	bool recursiveMode;

	List<string> failedSongs = new List<string>();

	public UIElements ui;
	[System.Serializable]
	public class UIElements
	{
		public Toggle hqmode;
		public ScrollRect trackCue;
		public Text cueCounter;
		[Space]
		public InputField track1Name;
		public InputField track2Name;
		public InputField track3Name;
		public InputField track4Name;
		public Image track1ColorPanel;
		public Image track2ColorPanel;
		public Image track3ColorPanel;
		public Image track4ColorPanel;
		[Space]
		public Toggle compressorOn;
		public Fader input;
		public Fader drywet;
		public Fader output;
		public Fader ratio;
		public Fader hpcutoff;
		public Fader attack;
		public Fader release;
		public Fader threshold;
		[Space]
		public Toggle limiteron;
		public Fader release2;
		public Fader threshold2;
		public Fader ceiling;
		[Space]
		public Slider hue;
		public Slider sat;
		public Slider val;
		public Image sample;
		[Space]
		[Space]
		[Space]
		public Button startButton;
		public Button pauseButton;
		
		public Slider totalProgress;
		public Text totalProgressText;
		public Slider trackProgress;
		public Text trackProgressText;

		public Text destinationText;
		public Image replaceToggle;

		public Text title;
		public Text artist;
		public Text album;
		public Text log;
		public Image artwork;
		public Image artwork2;
	}

	string titleTag;
	string albumTag;
	string artistTag;
	List<string> logText = new List<string>{"", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "Ready!"};

	string slTotalText;
	int slTotalMax;
	int slTotalCurrent;

	string slTrackText;
	int slTrackMax;
	int slTrackCurrent;

	bool replaceMode = true;

	byte[] artworkData = new byte[1];
	Texture2D tex2D;
	Sprite newSprite;

	[System.Serializable]
	public class Compressor
	{
		public float attack;
		public int dry_wet;
		public bool enabled;
		public int hp_cutoff;
		public float input_gain;
		public float output_gain;
		public float ratio;
		public float release;
		public float threshold;

	}
	[System.Serializable]
	public class Limiter
	{
		public float ceiling;
		public bool enabled;
		public float release;
		public float threshold;

	}
	[System.Serializable]
	public class MasteringDsp
	{
		public Compressor compressor;
		public Limiter limiter;

	}
	[System.Serializable]
	public class Stem
	{
		public string color;
		public string name;

	}
	[System.Serializable]
	public class StemData
	{
		public MasteringDsp mastering_dsp;
		public List<Stem> stems;
		public int version;

	}

	Tags trackTags = new Tags();
	[System.Serializable]
	public class Tags
	{
		public string track;
		public string artist;
		public string release;
		public string label;
		public string genre;

	}

	private void Awake()
	{
		
	}

	void Start()
	{
		dataPath = Application.dataPath;
		tempPath = Application.temporaryCachePath;
		tex2D = new Texture2D(2, 2);
		pivot.localPosition = new Vector3(0, 0, 0);
		SetStemOptionsUI();
	}

	public void SelectFileMode()
	{
		recursiveMode = false;
		SelectFiles();
	}

	public void SelectFolderMode()
	{
		recursiveMode = false;
		SelectFolder();
	}

	public void SelectRecursiveMode()
	{
		recursiveMode = true;
		SelectFolder();	
	}

	public void ChangePage(int direction)
	{
		pageNumber += (direction == 0 ? -pageNumber : direction);
		StartCoroutine(ChangePage());
	}

	IEnumerator ChangePage()
	{
		Vector3 startPos = pivot.localPosition;
		Vector3 endPos = new Vector3(-1920 * pageNumber, 0, 0);
		float ratio = 0;
		float duration = 0.5f;
		float multiplier = 1.0f / duration;
		while(pivot.localPosition != endPos)
		{
			ratio += Time.deltaTime * multiplier;
			pivot.localPosition = Vector3.Lerp(startPos, endPos, ratio);
			yield return new WaitForEndOfFrame();
		}
		yield return new WaitForEndOfFrame();
	}

	public void ResetStemOption()
	{
		stemData = defaultData;
		SetStemOptionsUI();
	}

	public void LoadStemOptions()
	{
		string[] location = StandaloneFileBrowser.OpenFilePanel("Open preset file", "", "json", false);
		if(location.Length > 0)
		{
			string loadedText = File.ReadAllText(location[0]);
			stemData = JsonUtility.FromJson<StemData>(loadedText);
			SetStemOptionsUI();
		}
	}

	public void SaveStemOption()
	{
		SaveStemOptionUI();
		string location = StandaloneFileBrowser.SaveFilePanel("Save preset location", "", "Preset", "json");
		File.WriteAllText(location, JsonUtility.ToJson(stemData));

	}

	void SetStemOptionsUI()
	{
		ui.track1Name.text = stemData.stems[0].name;
		ui.track2Name.text = stemData.stems[1].name;
		ui.track3Name.text = stemData.stems[2].name;
		ui.track4Name.text = stemData.stems[3].name;

		ui.track1ColorPanel.color = hexToRgb(stemData.stems[0].color);
		ui.track2ColorPanel.color = hexToRgb(stemData.stems[1].color);
		ui.track3ColorPanel.color = hexToRgb(stemData.stems[2].color);
		ui.track4ColorPanel.color = hexToRgb(stemData.stems[3].color);


		ui.compressorOn.isOn = stemData.mastering_dsp.compressor.enabled;
		ui.input.value = stemData.mastering_dsp.compressor.input_gain;
		ui.drywet.value = stemData.mastering_dsp.compressor.dry_wet;
		ui.output.value = stemData.mastering_dsp.compressor.output_gain;
		ui.ratio.value = stemData.mastering_dsp.compressor.ratio;
		ui.hpcutoff.value = stemData.mastering_dsp.compressor.hp_cutoff;
		ui.attack.value = stemData.mastering_dsp.compressor.attack;
		ui.release.value = stemData.mastering_dsp.compressor.release;
		ui.threshold.value = stemData.mastering_dsp.compressor.threshold;

		ui.limiteron.isOn = stemData.mastering_dsp.limiter.enabled;
		ui.release2.value = stemData.mastering_dsp.limiter.release;
		ui.threshold2.value = stemData.mastering_dsp.limiter.threshold;
		ui.ceiling.value = stemData.mastering_dsp.limiter.ceiling;
	}

	public void SaveStemOptionUI()
	{
		stemData.stems[0].name = ui.track1Name.text;
		stemData.stems[1].name = ui.track2Name.text;
		stemData.stems[2].name = ui.track3Name.text;
		stemData.stems[3].name = ui.track4Name.text;

		stemData.stems[0].color = rbgToHex(ui.track1ColorPanel.color);
		stemData.stems[1].color = rbgToHex(ui.track2ColorPanel.color);
		stemData.stems[2].color = rbgToHex(ui.track3ColorPanel.color);
		stemData.stems[3].color = rbgToHex(ui.track4ColorPanel.color);


		stemData.mastering_dsp.compressor.enabled = ui.compressorOn.isOn;
		stemData.mastering_dsp.compressor.input_gain = ui.input.value;
		stemData.mastering_dsp.compressor.dry_wet = (int)ui.drywet.value;
		stemData.mastering_dsp.compressor.output_gain = ui.output.value;
		stemData.mastering_dsp.compressor.ratio = ui.ratio.value;
		stemData.mastering_dsp.compressor.hp_cutoff = (int)ui.hpcutoff.value;
		stemData.mastering_dsp.compressor.attack = ui.attack.value;
		stemData.mastering_dsp.compressor.release = ui.release.value;
		stemData.mastering_dsp.compressor.threshold = ui.threshold.value;

		stemData.mastering_dsp.limiter.enabled = ui.limiteron.isOn;
		stemData.mastering_dsp.limiter.release = ui.release2.value;
		stemData.mastering_dsp.limiter.threshold = ui.threshold2.value;
		stemData.mastering_dsp.limiter.ceiling = ui.ceiling.value;
	}

	Color hexToRgb(string hex)
	{
		Color color = Color.red;
		ColorUtility.TryParseHtmlString(hex, out color);
		return color;
	}

	string rbgToHex(Color color)
	{
		return ( "#" + ColorUtility.ToHtmlStringRGB(color));
	}

	public void SetStemColor(int track)
	{
		switch(track)
		{
			case 0:
				ui.track1ColorPanel.color = ui.sample.color;
				break;
			case 1:
				ui.track2ColorPanel.color = ui.sample.color;
				break;
			case 2:
				ui.track3ColorPanel.color = ui.sample.color;
				break;
			case 3:
				ui.track4ColorPanel.color = ui.sample.color;
				break;
		}
	}

	public void HideShowLog()
	{
		ui.log.transform.parent.gameObject.SetActive(!ui.log.transform.parent.gameObject.activeSelf);
	}

	float gameTime;
	float timeScanDelta;
	bool setScanDeltaTime;
	void Update()
	{
		gameTime = Time.time;
		if(setScanDeltaTime)
		{
			timeScanDelta = gameTime;
			setScanDeltaTime = false;
		}
		highQualityMode = ui.hqmode.isOn;

		ui.sample.color = Color.HSVToRGB(ui.hue.value, ui.sat.value, ui.val.value);

		ui.totalProgress.maxValue = slTotalMax;
		ui.totalProgress.value = slTotalCurrent;
		ui.totalProgressText.text = "" + slTotalCurrent + " of " + conversionCue.Length;

		ui.trackProgress.maxValue = slTrackMax;
		ui.trackProgress.value = slTrackCurrent;
		ui.trackProgressText.text = slTrackText;

		ui.title.text = titleTag;
		ui.artist.text = artistTag;
		ui.album.text = albumTag;

		ui.replaceToggle.enabled = replaceMode;

		string newText = "";
		for(int x = 25; x > 0; x--)
		{
			newText += "" + logText[logText.Count - x] + '\n';
		}
		ui.log.text = newText;

		if(thread != null && thread.IsAlive)
		{
			ui.startButton.GetComponentInChildren<Text>().text = "Stop";
			ui.pauseButton.gameObject.SetActive(true);
		} else
		{
			ui.startButton.GetComponentInChildren<Text>().text = "Start";
			ui.pauseButton.gameObject.SetActive(false);
		}

		if(pauseState)
		{
			ui.pauseButton.transform.GetChild(0).gameObject.SetActive(true);
			ui.pauseButton.transform.GetChild(1).gameObject.SetActive(false);
		} else
		{
			ui.pauseButton.transform.GetChild(0).gameObject.SetActive(false);
			ui.pauseButton.transform.GetChild(1).gameObject.SetActive(true);
		}

	}

	public void SetPauseState()
	{
		pauseState = !pauseState;
		LogPrint(pauseState ? "Paused conversion" : "Conversion resumed");
	}

	private void FixedUpdate()
	{
		if(artworkData.Length > 255)
		{

			tex2D.LoadImage(artworkData);
			newSprite = Sprite.Create(tex2D, new Rect(0,0, tex2D.width, tex2D.height), new Vector2(0,0), 100.0f);
			ui.artwork.sprite = newSprite;
			ui.artwork2.sprite = newSprite;
		}
		
	}

	public void SetDestination()
	{
		try
		{
			outputPath = StandaloneFileBrowser.OpenFolderPanel("Destination folder", "", false)[0];
		} catch
		{
			replaceMode = true;
			outputPath = "";
			ui.destinationText.text = "Destination: Same as Original";
		}
		if(outputPath == "")
		{
			replaceMode = true;
			outputPath = "";
			ui.destinationText.text = "Destination: Same as Original";
		} else
		{
			replaceMode = false;
			ui.destinationText.text = "Destination: " + outputPath;
		}
	}

	public void SetReplaceToggle()
	{
		if(replaceMode)
		{
			replaceMode = false;
			if(outputPath == "")
			{
				SetDestination();
			} else
			{
				ui.destinationText.text = "Destination: " + outputPath;
			}
		} else
		{
			replaceMode = true;
			ui.destinationText.text = "Destination: Same as Original";
		}
	}

	public void SelectFiles()
	{
		conversionCue = StandaloneFileBrowser.OpenFilePanel("File(s) to Convert", "", "mp3", true);
		PopulateCueList();
		if(conversionCue.Length > 0)
		{
			ChangePage(1);
		}
	}

	public void SelectFolder()
	{
		string dir;
		try
		{
			dir = StandaloneFileBrowser.OpenFolderPanel("Folder to Convert", "", false)[0];
		} catch
		{
			return;
		}
		if(dir == ""){return;}
		songsFolder = dir;
		bool recursive = true;
		conversionCue = Directory.GetFiles(dir, "*.mp3",  recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
		PopulateCueList();
		if(conversionCue.Length > 0 || recursiveMode)
		{
			ChangePage(1);
		}
	}

	void PopulateCueList()
	{
		for(int y = 1; y < ui.trackCue.content.transform.childCount; y++)
		{
			Destroy(ui.trackCue.content.GetChild(y).gameObject);
		}

		GameObject template = ui.trackCue.content.transform.GetChild(0).gameObject;
		template.SetActive(false);
		float templateHeight = template.GetComponent<RectTransform>().sizeDelta.y;
		ui.trackCue.content.sizeDelta = new Vector2(0, templateHeight * conversionCue.Length);
		int x = 0;
		foreach(string str in conversionCue)
		{
			x++;
			GameObject newSong = Instantiate(template, ui.trackCue.content);
			if(x % 2 == 0)
			{
				newSong.transform.GetChild(0).GetComponent<Image>().color = Color.white;
			}
			newSong.transform.GetChild(1).GetComponent<Text>().text = ""+x;
			newSong.transform.GetChild(2).GetComponent<Text>().text = Path.GetFileNameWithoutExtension(str);
			newSong.transform.GetChild(3).GetComponent<Text>().text = str;
			newSong.SetActive(true);
			newSong.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, templateHeight * -(x-1));
		}
		ui.cueCounter.text = "" + conversionCue.Length + " Songs found." + (recursiveMode ? " Recursive mode is on!" : "");
	}

	public void StartStopConversion()
	{
		ConvertCue();
	}

	Thread thread;
	public void ConvertCue()
	{
		if(thread != null && thread.IsAlive)
		{
			thread.Abort();
			LogPrint("Conversion was stopped!");
			ChangePage(0);
			slTrackCurrent = 0;
			slTotalCurrent = 0;
			slTrackText = "Ready!";
			
		} else
		{
			pauseState = false;
			thread = new Thread(delegate ()
			{
				VerifyCue();
				ConvertCueStart();
			});
			thread.Start();
		}
	}

	
	void VerifyCue()
	{
		List<string> verifiedCue = new List<string>();
		int incom = 0;
		foreach(string str in conversionCue)
		{
			if(!IsFileLocked(new FileInfo(str)))
			{
				verifiedCue.Add(str);
				//UnityEngine.Debug.Log("File is ready! " + str);
			} else
			{
				incom++;
				//UnityEngine.Debug.Log("File is locked! " + str);
			}
		}
		conversionCue = verifiedCue.ToArray();
		if(incom > 0)
		{
			LogPrint("" + incom + " songs have not finished downloading yet. These will be skipped for now.");
		}
	}

	bool IsFileLocked(FileInfo file)
	{
		FileStream stream = null;

		try
		{
			stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
		} catch(IOException)
		{
			return true;
		} finally
		{
			if(stream != null)
				stream.Close();
		}

		//file is not locked
		return false;
	}

	void ConvertCueStart()
	{
		if(conversionCue.Length > 0)
		{
			slTotalMax = conversionCue.Length;
			slTotalCurrent = 0;
			for(int x = 0; x < conversionCue.Length; x++)
			{
				slTotalCurrent++;
				ConvertTrack(conversionCue[x]);
			}
			slTrackText = "Done!";
		}
		conversionCue = new string[] { };
		if(recursiveMode)
		{
			slTrackText = "Done! Scanning for new songs...";
			ScanForNewSongs();
		}
	}

	void ScanForNewSongs()
	{
		LogPrint("Finished Queue, scanning for new songs.");
		while(conversionCue.Length == 0)
		{
			if(gameTime - timeScanDelta > 5.0f)
			{
				conversionCue = Directory.GetFiles(songsFolder, "*.mp3", true ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
				setScanDeltaTime = true;
				VerifyCue();
			}
		}
		LogPrint("Found " + conversionCue.Length + " new songs!");
		ConvertCueStart();
	}

	private void OnApplicationQuit()
	{
		if(thread != null && thread.IsAlive)
		{
			thread.Abort();
			Cleanup();
			LogPrint("Process was aborted.");
		}
	}

	void ConvertTrack(string newTrackpath)
	{
		try
		{
			trackPath = newTrackpath;
			slTrackMax = 12 + 4;
			slTrackCurrent = 0;

			while(pauseState){ }
				
			LogPrint("Cleaning up files...");
			slTrackText = "Starting...";
			slTrackCurrent++;
			Cleanup();
			LogPrint("	Done");

			while(pauseState){ }

			LogPrint("Creating temporary directory...");
			slTrackText = "Creating temporary directory...";
			slTrackCurrent++;
			CreateTempDir();
			LogPrint("	Done");

			while(pauseState){ }

			LogPrint("Extracting metadata from track...");
			slTrackText = "Extracting metadata from track...";
			slTrackCurrent++;
			ExtractMetadata(trackPath);
			LogPrint("	Done");

			while(pauseState){ }

			LogPrint("Extracting album art from track...");
			slTrackText = "Extracting album art from track...";
			slTrackCurrent++;
			ExtractAlbumArt(trackPath);
			LogPrint("	Done");

			SetTrackUIData();
			while(pauseState){ }

			LogPrint("Converting track to WAV...");
			slTrackText = "Converting track to WAV...";
			slTrackCurrent++;
			ConvertToWav(trackPath);
			LogPrint("	Done");

			while(pauseState){ }

			LogPrint("Splitting WAV into STEMS...");
			slTrackText = "Splitting WAV into STEMS...";
			slTrackCurrent++;
			Split();
			LogPrint("	Done");

			while(pauseState){ }

			LogPrint("Converting STEMS to MP4...");
			slTrackText = "Converting STEMS to MP4...";
			slTrackCurrent++;
			ConvertStems();
			LogPrint("	Done");

			while(pauseState){ }

			LogPrint("Compiling Tags into JSON file...");
			slTrackText = "Compiling Tags into JSON file...";
			slTrackCurrent++;
			WriteTagJSON();
			LogPrint("	Done");

			while(pauseState){ }

			LogPrint("Compiling STEMS to single file...");
			slTrackText = "Compiling STEMS to single file...";
			slTrackCurrent++;
			CompileStem();
			LogPrint("	Done");

			while(pauseState){ }

			LogPrint("Applying album art...");
			slTrackText = "Applying album art...";
			slTrackCurrent++;
			ApplyAlbumArt(trackPath);
			LogPrint("	Done");

			while(pauseState){ }

			LogPrint("Moving file...");
			slTrackText = "Moving file...";
			slTrackCurrent++;
			RenameAndMove(replaceMode ? Path.GetDirectoryName(trackPath) : outputPath);
			LogPrint("	Done");

			while(pauseState){ }

			if(replaceMode)
			{
				LogPrint("Deleting original file...");
				slTrackText = "Deleting original file...";
				RemoveOriginal();
				LogPrint("	Done");
			}

			while(pauseState){ }

			LogPrint("Cleaning up files...");
			slTrackText = "Cleaning up files...";
			slTrackCurrent++;
			Cleanup();
			LogPrint("	Done");


		} catch(System.Exception ex)
		{
			LogPrint(ex.ToString());
			try
			{
				Cleanup();
			} catch
			{

			}
		}

		LogPrint("Verifing results...");
		bool fileHealth = Verify(replaceMode ? Path.GetDirectoryName(trackPath) : outputPath);
		LogPrint(fileHealth ? "	File Converted successfuly!" : "	ERROR: There was a problem while converting. Maybe the song is too long?");
		if(!fileHealth)
		{
			failedSongs.Add(trackPath);
		}
		LogPrint("	Done");
	}

	void LogPrint(string str)
	{
		UnityEngine.Debug.Log(str);
		logText.Add(str);
	}

	void SetTrackUIData()
	{
		titleTag = trackTags.track;
		artistTag = trackTags.artist;
		albumTag = trackTags.release;

		string path = dataPath + "/temp/cover.png";
		if(File.Exists(path))
		{
			artworkData = File.ReadAllBytes(path);
		}
	}

	public void Cleanup()
	{
		if(Directory.Exists(dataPath + "/temp/stems"))
		{
			Directory.Delete(dataPath + "/temp/stems", true);
		}

		string[] pathsDel = {"/temp/cover.png","/temp/metadata.json","/temp/metadata.txt","/temp/tags.json","/temp/track.mp4","/temp/track.wav"};
		foreach(string str in pathsDel)
		{
			if(File.Exists(dataPath + str))
			{
				File.Delete(dataPath + str);
			}
		}
		
		if(Directory.Exists(Directory.GetParent(Directory.GetParent(tempPath).ToString()) + "/serving"))
		{
			try
			{
				Directory.Delete(Directory.GetParent(Directory.GetParent(tempPath).ToString()) + "/serving", true);
			} catch { }
		}

		string[] badFiles = Directory.GetFiles(Directory.GetParent(Directory.GetParent(tempPath).ToString()).ToString(), "gpac*");
		foreach(string bf in badFiles)
		{
			File.Delete(bf);
		}
		LogPrint("	Deleted " + badFiles.Length + " temp files.");
	}

	public void CreateTempDir()
	{
		Directory.CreateDirectory(dataPath + "/temp/");
	}

	public void ExtractMetadata(string track)
	{	
		string command = " -i " + '"' + track + '"' + " -f ffmetadata " + '"' + dataPath + "/temp/metadata.txt" + '"';
		ProcessStartInfo processStart = new ProcessStartInfo("ffmpeg.exe");
		processStart.Arguments = command;
		if(!debugMode){processStart.WindowStyle = ProcessWindowStyle.Hidden;}
		Process process = Process.Start(processStart);
		process.WaitForExit();
		process.Close();

		string[] metadata = File.ReadAllLines(dataPath + "/temp/metadata.txt");
		foreach(string line in metadata)
		{
			string[] sides = line.Split('=');

			switch(sides[0])
			{
				case "title":
					trackTags.track = sides[1];
					break;

				case "artist":
					trackTags.artist = sides[1];
					break;

				case "album":
					trackTags.release = sides[1];
					break;

				case "genre":
					trackTags.genre = sides[1];
					break;
			}
		}
	}

	public void ConvertToWav(string track)
	{
		string command = " -i " + '"' + track + '"' + " -ar 44100 " + '"' + dataPath + "/temp/track.wav" + '"';
		ProcessStartInfo processStart = new ProcessStartInfo("ffmpeg.exe");
		processStart.Arguments = command;
		if(!debugMode){processStart.WindowStyle = ProcessWindowStyle.Hidden;}
		Process process = Process.Start(processStart);
		process.WaitForExit();
		process.Close();
	}

	public void Split()
	{
		string cmd = "cd " + '"' + dataPath + "/temp/" + '"';
		string cmd2 = "spleeter separate -i " + '"' + dataPath + "/temp/track.wav" + '"' + " -o stems " + (highQualityMode ? "-p spleeter:4stems-16kHz" : "-p spleeter:4stems");
		ProcessStartInfo process = new ProcessStartInfo("cmd.exe");
		process.Arguments = "/c " + cmd + " & " + cmd2;
		if(!debugMode){process.WindowStyle = ProcessWindowStyle.Hidden;}
		Process processT = Process.Start(process);
		processT.WaitForExit();
		processT.Close();
	}

	public void ConvertStems()
	{
		string[] stemNames = {"drums", "bass", "other", "vocals"};
		foreach(string name in stemNames)
		{
			LogPrint("		Converting " + name + " to MP4");
			string command = " -i " + '"' + dataPath + "/temp/stems/track/" + name + ".wav" + '"' + " -ar 44100 " + '"' + dataPath + "/temp/stems/track/" + name + ".mp4" + '"';
			ProcessStartInfo processStart = new ProcessStartInfo("ffmpeg.exe");
			processStart.Arguments = command;
			if(!debugMode){processStart.WindowStyle = ProcessWindowStyle.Hidden;}
			Process process = Process.Start(processStart);
			process.WaitForExit();
			process.Close();
			slTrackCurrent++;
			LogPrint("			Done");
		}
		LogPrint("		Converting track to MP4");
		string command2 = " -i " + '"' + dataPath + "/temp/track.wav" + '"'  + " -ar 44100 " + '"' + dataPath + "/temp/track.mp4" + '"';
		ProcessStartInfo processStart2 = new ProcessStartInfo("ffmpeg.exe");
		processStart2.Arguments = command2;
		if(!debugMode){processStart2.WindowStyle = ProcessWindowStyle.Hidden;}
		Process process2 = Process.Start(processStart2);	
		process2.WaitForExit();
		process2.Close();
		LogPrint("			Done");
	}

	public void CompileStem()
	{
		WriteStemJSON();

		string stemsPath = dataPath + "/temp/stems/track";
		string command = " create " + "-s" + " " + '"' + stemsPath + "/drums.mp4" + '"' + " " + '"' + stemsPath + "/bass.mp4" + '"' + " " + '"' + stemsPath + "/other.mp4" + '"' + " " + '"' + stemsPath + "/vocals.mp4" + '"' + " -x " + '"' + dataPath + "/temp/track.mp4" + '"' + " -m " + '"' + dataPath + "/temp/metadata.json" + '"' + " -t " + '"' + dataPath + "/temp/tags.json" + '"';
		ProcessStartInfo processStart = new ProcessStartInfo(dataPath + "/tools/ni-stem/ni-stem.exe");
		processStart.Arguments = command;
		if(!debugMode){processStart.WindowStyle = ProcessWindowStyle.Hidden;}
		Process process = Process.Start(processStart);		
		process.WaitForExit();
		process.Close();
	}

	void WriteStemJSON()
	{
		string path = dataPath + "/temp/metadata.json";
		if(File.Exists(path))
		{
			File.Delete(path);
		}
		var json = File.CreateText(path);
		json.Write(JsonUtility.ToJson(stemData));
		json.Close();
	}

	void WriteTagJSON()
	{
		string path = dataPath + "/temp/tags.json";
		if(File.Exists(path))
		{
			File.Delete(path);
		}
		var json = File.CreateText(path);
		json.Write(JsonUtility.ToJson(trackTags));
		json.Close();
	}

	public void ExtractAlbumArt(string track)
	{
		string command = " -i " + '"' + track + '"' + " -an -vcodec copy " + '"' + dataPath + "/temp/cover.png" + '"';
		ProcessStartInfo processStart = new ProcessStartInfo("ffmpeg.exe");
		processStart.Arguments = command;
		if(!debugMode){processStart.WindowStyle = ProcessWindowStyle.Hidden;}
		Process process = Process.Start(processStart);
		process.WaitForExit();
		process.Close();
	}

	public void ApplyAlbumArt(string track)
	{
		string command = " --add " + '"' + dataPath + "/temp/cover.png" + '"' + " " + '"' + dataPath + "/temp/track.stem.m4a" + '"';
		ProcessStartInfo processStart = new ProcessStartInfo(dataPath + "/tools/mp4v2/mp4art.exe");
		processStart.Arguments = command;
		if(!debugMode){processStart.WindowStyle = ProcessWindowStyle.Hidden;}
		Process process = Process.Start(processStart);
		process.WaitForExit();
		process.Close();
	}

	string lastMovePath;
	public void RenameAndMove(string destinationPath)
	{
		string newPath = destinationPath + "/" + Path.GetFileNameWithoutExtension(trackPath) + ".stem.m4a";
		File.Delete(newPath);
		File.Move(dataPath + "/temp/track.stem.m4a", newPath);
		lastMovePath = newPath;
	}

	bool Verify(string destinationPath)
	{
		string newPath = destinationPath + "/" + Path.GetFileNameWithoutExtension(trackPath) + ".stem.m4a";
		UnityEngine.Debug.Log("Verifying file " + newPath);
		return File.Exists(newPath);
	}

	public void RemoveOriginal()
	{
		File.Delete(trackPath);
	}
}
