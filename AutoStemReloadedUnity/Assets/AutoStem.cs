using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Threading;
using System.IO;
using SFB;
using UnityEngine.UI;

public class AutoStem : MonoBehaviour
{
	public RectTransform pivot;
	int pageNumber = 0;
	public bool debugMode;
	[Space]
	public int fileSplitSize = 60;
	public bool convertFails;
	public bool highQualityMode;
	public bool saveAsMp4;
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

	///////////////////////////////
	public int batchSize = 10;
	public List<SongBatch> batchCue = new List<SongBatch>();
	[System.Serializable]
	public class SongBatch
	{
		public List<string> songs = new List<string>();
		public List<string> names = new List<string>();
	}
	public List<string> failedSongs = new List<string>();
	///////////////////////////////

	public UIElements ui;
	[System.Serializable]
	public class UIElements
	{
		public Text versionText;
		public GameObject helpSceen;
		public Toggle hqmode;
		public Toggle mp4mode;
		public Toggle convertFails;
		public Dropdown batchSize;
		public ScrollRect trackCue;
		public Text cueCounter;
		public Dropdown failedSongs;
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

	string toolPath;

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

	public void ShowHideHelp()
	{
		ui.helpSceen.SetActive(!ui.helpSceen.activeSelf);
	}

	void Start()
	{
		ui.versionText.text = "v " + Application.version;
		dataPath = Application.dataPath;
		tempPath = Application.temporaryCachePath;
		tex2D = new Texture2D(2, 2);
		pivot.localPosition = new Vector3(0, 0, 0);
		SetStemOptionsUI();
		if(Application.isEditor)
		{
			toolPath = Directory.GetParent(dataPath).ToString() + "/tools";
		} else
		{
			toolPath = dataPath + "/tools";
		}
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
		saveAsMp4 = ui.mp4mode.isOn;
		convertFails = ui.convertFails.isOn;

		if(failedSongs.Count > 0)
		{
			ui.failedSongs.gameObject.SetActive(true);
			ui.failedSongs.ClearOptions();
			List<Dropdown.OptionData> dod = new List<Dropdown.OptionData>();
			foreach(string str in failedSongs)
			{
				Dropdown.OptionData opd = new Dropdown.OptionData();
				opd.text = str;
				dod.Add(opd);
			}
			ui.failedSongs.AddOptions(dod);
		} else
		{
			ui.failedSongs.gameObject.SetActive(false);
		}

		switch(ui.batchSize.value)
		{
			case 0:
				batchSize = 100;
				break;
			case 1:
				batchSize = 50;
				break;
			case 2:
				batchSize = 25;
				break;
			case 3:
				batchSize = 10;
				break;
			case 4:
				batchSize = 5;
				break;
			case 5:
				batchSize = 3;
				break;
			case 6:
				batchSize = 1;
				break;
			case 7:
				batchSize = 0;
				break;
		}

		ui.sample.color = Color.HSVToRGB(ui.hue.value, ui.sat.value, ui.val.value);

		ui.totalProgress.maxValue = slTotalMax;
		ui.totalProgress.value = slTotalCurrent;
		ui.totalProgressText.text = (failedSongs.Count > 0 ? "["+failedSongs.Count+"] " : "") + "" + slTotalCurrent + " of " + slTotalMax;

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
			outputPath = StandaloneFileBrowser.OpenFolderPanel("Output folder", "", false)[0];
		} catch
		{
			replaceMode = true;
			outputPath = "";
			ui.destinationText.text = "Output Folder: Same as Original";
		}
		if(outputPath == "")
		{
			replaceMode = true;
			outputPath = "";
			ui.destinationText.text = "Output Folder: Same as Original";
		} else
		{
			replaceMode = false;
			ui.destinationText.text = "Output Folder: " + outputPath;
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
				ui.destinationText.text = "Output Folder: " + outputPath;
			}
		} else
		{
			replaceMode = true;
			ui.destinationText.text = "Output Folder: Same as Original";
		}
	}

	public void SelectFiles()
	{
		conversionCue = StandaloneFileBrowser.OpenFilePanel("File(s) to Convert", "", "mp3", true);
		VerifyCue();
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
		VerifyCue();
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
				slTrackCurrent = 0;
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

		batchCue = new List<SongBatch>();
		int batchSizeReal = (batchSize == 0 ? conversionCue.Length : batchSize);
		for(int x = 0; x < conversionCue.Length; x++)
		{
			if(x % batchSizeReal == 0)
			{
				batchCue.Add(new SongBatch());
			}
			string path = conversionCue[x];
			batchCue[batchCue.Count - 1].songs.Add(path);
			batchCue[batchCue.Count - 1].names.Add(Path.GetFileNameWithoutExtension(path));
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
			failedSongs.Clear();
			slTotalMax = conversionCue.Length;
			slTotalCurrent = 0;
			for(int x = 0; x < batchCue.Count; x++)
			{
				SplitBatch(x);
				UnityEngine.Debug.Log("Splitbatch done");
				for(int y = 0; y < batchCue[x].songs.Count; y++)
				{
					ConvertTrack(x, y);
					//failedSongs.Add(batchCue[x].songs[y]);//DEBUG DEBUG DEBUG DEBUG DEBUG DEBUG DEBUG DEBUG DEBUG DEBUG DEBUG 
				}
			}

			if(convertFails)
			{
				List<string> songsToFix = new List<string>();
				songsToFix.AddRange(failedSongs);
				for(int x = 0; x < songsToFix.Count; x++)
				{
					ConvertLongSong(songsToFix, x);
				}
			}

			LogPrint("Cleaning up files...");
			slTrackText = "Cleaning up files...";
			slTrackCurrent++;
			Cleanup();
			LogPrint("	Done");

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

	void SplitBatch(int batch)
	{
		while(pauseState){ }
		LogPrint("Cleaning up files...");
		slTrackText = "Starting...";
		Cleanup();
		LogPrint("	Done");

		while(pauseState){ }
		LogPrint("Creating temporary directory...");
		slTrackText = "Creating temporary directory...";
		CreateTempDir();
		LogPrint("	Done");

		while(pauseState){ }
		LogPrint("Splitting " + batchCue[batch].songs.Count + " tracks into STEMS...");
		slTrackText = "Splitting tracks into STEMS...";
		Split(batch);
		LogPrint("	Done");
	}

	void ConvertTrack(int batchNum, int songNum)
	{
		try
		{
			trackPath = batchCue[batchNum].songs[songNum];
			slTrackMax = 7 + 4;
			slTrackCurrent = 0;

			string stemfolder = "" + dataPath + "/temp/stems/" + batchCue[batchNum].names[songNum] + "/";
			bool stemsExist = (File.Exists(stemfolder + "drums.wav") && File.Exists(stemfolder + "bass.wav") && File.Exists(stemfolder + "other.wav") && File.Exists(stemfolder + "vocals.wav"));

			if(stemsExist)
			{

				while(pauseState)
				{ }

				LogPrint("Extracting metadata from track...");
				slTrackText = "Extracting metadata from track...";
				slTrackCurrent++;
				ExtractMetadata(batchNum, songNum);
				LogPrint("	Done");

				while(pauseState)
				{ }

				LogPrint("Extracting album art from track...");
				slTrackText = "Extracting album art from track...";
				slTrackCurrent++;
				ExtractAlbumArt(batchNum, songNum);
				LogPrint("	Done");

				SetTrackUIData();

				while(pauseState)
				{ }

				LogPrint("Converting STEMS to MP4...");
				slTrackText = "Converting STEMS to MP4...";
				slTrackCurrent++;
				ConvertStems(batchNum, songNum);
				LogPrint("	Done");

				while(pauseState)
				{ }

				LogPrint("Compiling Tags into JSON file...");
				slTrackText = "Compiling Tags into JSON file...";
				slTrackCurrent++;
				WriteTagJSON();
				LogPrint("	Done");

				while(pauseState)
				{ }

				LogPrint("Compiling STEMS to single file...");
				slTrackText = "Compiling STEMS to single file...";
				slTrackCurrent++;
				CompileStem(batchNum, songNum);
				LogPrint("	Done");

				while(pauseState)
				{ }

				LogPrint("Applying album art...");
				slTrackText = "Applying album art...";
				slTrackCurrent++;
				ApplyAlbumArt(batchNum, songNum);
				LogPrint("	Done");

				while(pauseState)
				{ }

				LogPrint("Moving file...");
				slTrackText = "Moving file...";
				slTrackCurrent++;
				RenameAndMove(replaceMode ? Path.GetDirectoryName(trackPath) : outputPath, batchNum, songNum);
				LogPrint("	Done");

				while(pauseState)
				{ }

				if(replaceMode)
				{
					LogPrint("Deleting original file...");
					slTrackText = "Deleting original file...";
					RemoveOriginal(batchNum, songNum);
					LogPrint("	Done");
				}

			}

			Cleanup(true);
			string stemdir = dataPath + "/temp/stems/" + batchCue[batchNum].names[songNum];
			if(Directory.Exists(stemdir))
			{
				Directory.Delete(stemdir, true);
			}

		} catch(System.Exception ex)
		{
			Cleanup(true);
			LogPrint(ex.ToString());
		}

		LogPrint("Verifing results...");
		bool fileHealth = Verify(replaceMode ? Path.GetDirectoryName(trackPath) : outputPath, batchNum, songNum);
		LogPrint(fileHealth ? "	File Converted successfuly!" : "	ERROR: There was a problem while converting. Maybe the song is too long?");
		if(!fileHealth)
		{
			failedSongs.Add(trackPath);
		} else
		{
			slTotalCurrent++;
		}
		LogPrint("	Done");
	}


	void ConvertLongSong(List<string> list, int listNum)
	{
		Cleanup();
		string track = list[listNum];
		try
		{
			slTrackText = "Converting long track...";
			LogPrint("Converting track in long mode... " + track);
			//separate mp3 into smaller mp3s : trackFolder = dataPath + "/temp/splits";

			slTrackText = "Splitting failed track into segments...";
			LogPrint("   Splitting track into segments... " + track);
			SplitTrack(track);
			slTrackText = "Done";
			LogPrint("   Done" + track);

			//Add small mp3s into a batch

			batchCue.Clear();
			SongBatch newBatch = new SongBatch();
			string[] parts = Directory.GetFiles(dataPath + "/temp/splits");
			foreach(string str in parts)
			{
				newBatch.songs.Add(str);
				newBatch.names.Add(Path.GetFileNameWithoutExtension(str));
			}
			batchCue.Add(newBatch);

			slTrackText = "Converting segments into STEMS...";
			LogPrint("   Converting track segments into STEMS... ");
			Split(0);
			slTrackText = "Done";
			LogPrint("   Done" + track);

			slTrackText = "Merging segments into single STEMS...";
			LogPrint("   Merging segments into single STEMS... ");
			MergeSplitStems(track);
			slTrackText = "Done";
			LogPrint("   Done");

			Directory.Delete(dataPath + "/temp/splits", true);

			batchCue.Clear();
			SongBatch songBatch = new SongBatch();
			songBatch.songs.Add(track);
			songBatch.names.Add(Path.GetFileNameWithoutExtension(track));
			batchCue.Add(songBatch);

			ConvertTrack(0, 0);

			failedSongs.Remove(track);			
		} catch
		{
			LogPrint("   ERROR: could not convert long song!");
		}
	}

	void LogPrint(string str)
	{
		UnityEngine.Debug.Log(str);
		logText.Add(str);
	}

	void MergeSplitStems(string track)
	{
		string trackFolder = dataPath + "/temp/stems/";
		string[] stemfolder = Directory.GetDirectories(trackFolder, "part*", SearchOption.TopDirectoryOnly);

		List<string> drumfiles = new List<string>();
		List<string> bassfiles = new List<string>();
		List<string> otherfiles = new List<string>();
		List<string> voxfiles = new List<string>();

		foreach(string str in stemfolder)
		{
			UnityEngine.Debug.LogError("     " + str);
			drumfiles.Add(str + "/drums.wav");
			bassfiles.Add(str + "/bass.wav");
			otherfiles.Add(str + "/other.wav");
			voxfiles.Add(str + "/vocals.wav");
		}

		string joinedStemFolder = dataPath + "/temp/stems/" + Path.GetFileNameWithoutExtension(track);
		if(Directory.Exists(joinedStemFolder))
		{
			Directory.Delete(joinedStemFolder, true);
		}
		Directory.CreateDirectory(joinedStemFolder);

		drumfiles.Sort();
		LogPrint("      Merging Drums... ");
		MergeWAVs(drumfiles.ToArray(), joinedStemFolder + "/drums.wav");
		LogPrint("         Done");

		bassfiles.Sort();
		LogPrint("      Merging Bass... ");
		MergeWAVs(bassfiles.ToArray(), joinedStemFolder + "/bass.wav");
		LogPrint("         Done");

		otherfiles.Sort();
		LogPrint("      Merging Other... ");
		MergeWAVs(otherfiles.ToArray(), joinedStemFolder + "/other.wav");
		LogPrint("         Done");

		voxfiles.Sort();
		LogPrint("      Merging Vocals... ");
		MergeWAVs(voxfiles.ToArray(), joinedStemFolder + "/vocals.wav");
		LogPrint("         Done");
	}

	void MergeWAVs(string[] wavFiles, string output)
	{
		string command = "";
		foreach(string str in wavFiles)
		{
			command += '"' + str + '"' + " ";
		}
		command += '"' + output + '"';
		ProcessStartInfo processStart = new ProcessStartInfo(toolPath + "/sox/sox.exe");
		UnityEngine.Debug.Log(command);
		processStart.Arguments = command;
		if(!debugMode)
		{ processStart.WindowStyle = ProcessWindowStyle.Hidden; }
		Process process = Process.Start(processStart);
		process.WaitForExit();
		process.Close();
	}

	void SplitTrack(string track, bool wavMethod = true)
	{
		if(wavMethod)
		{
			//C:\Users\patbe\Desktop\AutoStemReloaded\Builds\AutoStem_Data\tools\sox\sox.exe "C:\Users\patbe\Downloads\Shopping w_ Paranoid.wav" "C:\Users\patbe\Downloads\test\split.wav" trim 0 60 : newfile : restart
			//dataPath + "/temp/track.wav"
			string trackFolder = dataPath + "/temp/splits";
			if(Directory.Exists(trackFolder))
			{
				Directory.Delete(trackFolder, true);
			}
			Directory.CreateDirectory(trackFolder);

			ConvertToWav(track);

			string command = '"' + dataPath + "/temp/track.wav" + '"' + " " + '"' + trackFolder + "/part.wav" + '"' + " trim 0 " + fileSplitSize + " : newfile : restart";
			UnityEngine.Debug.Log("Wav method: " + command);
			ProcessStartInfo processStart = new ProcessStartInfo(toolPath + "/sox/sox.exe");
			processStart.Arguments = command;
			if(!debugMode)
			{ processStart.WindowStyle = ProcessWindowStyle.Hidden; }
			Process process = Process.Start(processStart);
			process.WaitForExit();
			process.Close();

			File.Delete(dataPath + "/temp/track.wav");
		} else
		{
			//ffmpeg -i "C:\Users\patbe\Downloads\10 Days.mp3" -f segment -segment_time 60 -c copy "C:\Users\patbe\Downloads\test\part%02d.mp3
			string trackFolder = dataPath + "/temp/splits";
			if(Directory.Exists(trackFolder))
			{
				Directory.Delete(trackFolder, true);
			}
			Directory.CreateDirectory(trackFolder);
			string command = " -i " + '"' + track + '"' + " -f segment -segment_time " + fileSplitSize + " -c copy " + '"' + trackFolder + "/part%03d.mp3" + '"';
			UnityEngine.Debug.Log(command);
			ProcessStartInfo processStart = new ProcessStartInfo("ffmpeg.exe");
			processStart.Arguments = command;
			if(!debugMode)
			{ processStart.WindowStyle = ProcessWindowStyle.Hidden; }
			Process process = Process.Start(processStart);
			process.WaitForExit();
			process.Close();
		}
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

	public void Cleanup(bool simple = false)
	{

		string[] pathsDel = { "/temp/cover.png", "/temp/metadata.json", "/temp/metadata.txt", "/temp/tags.json", "/temp/track.mp4", "/temp/track.wav" };
		foreach(string str in pathsDel)
		{
			if(File.Exists(dataPath + str))
			{
				File.Delete(dataPath + str);
			}
		}

		if(simple)
		{
			return;
		}

		if(Directory.Exists(dataPath + "/temp/splits"))
		{
			UnityEngine.Debug.Log("Deleted splits folder!");
			Directory.Delete(dataPath + "/temp/splits", true);
		}

		if(Directory.Exists(dataPath + "/temp/stems"))
		{
			UnityEngine.Debug.Log("Deleted stems folder!");
			Directory.Delete(dataPath + "/temp/stems", true);
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

	public void ExtractMetadata(int batchNum, int songNum)
	{
		string track = batchCue[batchNum].songs[songNum];
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

	public void Split(int batch)
	{
		string cmd = "cd " + '"' + dataPath + "/temp/" + '"';
		string cmd2 = "spleeter separate ";
		foreach(string str in batchCue[batch].songs)
		{
			cmd2 += '"' + str + '"' + " ";
		}
		cmd2 += " -o stems " + (highQualityMode ? "-p spleeter:4stems-16kHz" : "-p spleeter:4stems");// + " -f track/{instrument}.wav";
		UnityEngine.Debug.Log(cmd2);
		ProcessStartInfo process = new ProcessStartInfo("cmd.exe");
		process.Arguments = "/c " + cmd + " & " + cmd2;
		if(!debugMode){process.WindowStyle = ProcessWindowStyle.Hidden;}
		Process processT = Process.Start(process);
		processT.WaitForExit();
		processT.Close();
	}

	public void ConvertStems(int batchNum, int songNum)
	{
		string[] stemNames = {"drums", "bass", "other", "vocals"};
		string folder = "" + dataPath + "/temp/stems/" + batchCue[batchNum].names[songNum] + "/";
		foreach(string name in stemNames)
		{
			LogPrint("		Converting " + name + " to MP4");
			string command = " -i " + '"' + folder + name + ".wav" + '"' + " -ar 44100 " + '"' + folder + name + ".mp4" + '"';
			ProcessStartInfo processStart = new ProcessStartInfo("ffmpeg.exe");
			processStart.Arguments = command;
			if(!debugMode){processStart.WindowStyle = ProcessWindowStyle.Hidden;}
			Process process = Process.Start(processStart);
			process.WaitForExit();
			process.Close();
			slTrackCurrent++;
			LogPrint("			Done");
		}
		ConvertToWav(batchCue[batchNum].songs[songNum]);
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

	public void CompileStem(int batchNum, int songNum)
	{
		WriteStemJSON();

		string stemsPath = dataPath + "/temp/stems/" + batchCue[batchNum].names[songNum];
		string command = " create " + "-s" + " " + '"' + stemsPath + "/drums.mp4" + '"' + " " + '"' + stemsPath + "/bass.mp4" + '"' + " " + '"' + stemsPath + "/other.mp4" + '"' + " " + '"' + stemsPath + "/vocals.mp4" + '"' + " -x " + '"' + dataPath + "/temp/track.mp4" + '"' + " -m " + '"' + dataPath + "/temp/metadata.json" + '"' + " -t " + '"' + dataPath + "/temp/tags.json" + '"';// + " -o " + '"' + dataPath + "/temp/track.stem.m4a" + '"';
		ProcessStartInfo processStart = new ProcessStartInfo(toolPath + "/ni-stem/ni-stem.exe");
		UnityEngine.Debug.Log(command);
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

	public void ExtractAlbumArt(int batchNum, int songNum)
	{
		string track = batchCue[batchNum].songs[songNum];
		string command = " -i " + '"' + track + '"' + " -an -vcodec copy " + '"' + dataPath + "/temp/cover.png" + '"';
		ProcessStartInfo processStart = new ProcessStartInfo("ffmpeg.exe");
		processStart.Arguments = command;
		if(!debugMode){processStart.WindowStyle = ProcessWindowStyle.Hidden;}
		Process process = Process.Start(processStart);
		process.WaitForExit();
		process.Close();
	}

	public void ApplyAlbumArt(int batchNum, int songNum)
	{
		string track = batchCue[batchNum].songs[songNum];
		string command = " --add " + '"' + dataPath + "/temp/cover.png" + '"' + " " + '"' + dataPath + "/temp/track.stem.m4a" + '"';
		ProcessStartInfo processStart = new ProcessStartInfo(toolPath + "/mp4v2/mp4art.exe");
		processStart.Arguments = command;
		if(!debugMode){processStart.WindowStyle = ProcessWindowStyle.Hidden;}
		Process process = Process.Start(processStart);
		process.WaitForExit();
		process.Close();
	}

	string lastMovePath;
	public void RenameAndMove(string destinationPath, int batchNum, int songNum)
	{
		string newPath = destinationPath + "/" + batchCue[batchNum].names[songNum] + ".stem" + (saveAsMp4 ? ".mp4" : ".m4a");
		File.Delete(newPath);
		File.Move(dataPath + "/temp/track.stem.m4a", newPath);
		lastMovePath = newPath;
	}

	bool Verify(string destinationPath, int batchNum, int songNum)
	{
		string newPath = destinationPath + "/" + batchCue[batchNum].names[songNum] + ".stem" + (saveAsMp4 ? ".mp4" : ".m4a");
		UnityEngine.Debug.Log("Verifying file " + newPath);
		return File.Exists(newPath);
	}

	public void RemoveOriginal(int batchNum, int songNum)
	{
		if(File.Exists(batchCue[batchNum].songs[songNum]))
		{
			File.Delete(batchCue[batchNum].songs[songNum]);
		}
	}
}
