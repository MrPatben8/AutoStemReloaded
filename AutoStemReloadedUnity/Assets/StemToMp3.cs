using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using SFB;
using UnityEngine.UI;
using System.Linq;
using System.Diagnostics;
using System.Threading;

public class StemToMp3 : MonoBehaviour
{
    public AutoStem core;
    public List<string> files;
    public string searchPath;
    public bool deleteAfterConversion;
    public Slider progressBar;
    public Text sliderText;
    public Button startButton;
    public Button folderButton;
    public Button fileButton;
    public Toggle deleteToggle;

    public Text indicatorText;
    public Text pathText;

    bool done;

	private void Start()
	{
        pathText.text = "";
        indicatorText.text = "No files selected";
        done = true;
    }

	private void Update()
	{
        deleteAfterConversion = deleteToggle.isOn;
        progressBar.value = progress;
        sliderText.text = "" + progress + " of " + files.Count;

        startButton.GetComponentInChildren<Text>().text = done ? "Start Conversion" : "STOP";
        deleteToggle.interactable = done;
        folderButton.interactable = done;
        fileButton.interactable = done;

        if(progress < files.Count)
        {
            pathText.text = files[progress];
        }
    }

    public void SelectFiles()
	{
        files = new List<string>();
        List<ExtensionFilter> extensionFilter = new List<ExtensionFilter>();
        extensionFilter.Add(new ExtensionFilter("STEM", new string[] { "mp4", "m4a" }));

        files = new List<string>(StandaloneFileBrowser.OpenFilePanel("Search In", "", extensionFilter.ToArray(), true));
        indicatorText.text = "" + files.Count() + " Files Selected";
        //RenderList();
    }

    public void SelectDirectory()
    {
        files = new List<string>();
        searchPath = StandaloneFileBrowser.OpenFolderPanel("Search In", "", false)[0];
        GetFiles();
        indicatorText.text = "" + files.Count() + " Files Found";
        //RenderList();
    }

    void GetFiles()
    {
        files.Clear();
        files.AddRange(Directory.GetFiles(searchPath, "*.mp4", SearchOption.AllDirectories));
        files.AddRange(Directory.GetFiles(searchPath, "*.m4a", SearchOption.AllDirectories));
    }

    Thread thread;
    int progress = 0;
    public void ConvertList()
	{
        if(thread != null && thread.IsAlive)
        {
            thread.Abort();
            startButton.GetComponentInChildren<Text>().text = "Start Conversion";
            done = true;

        } else
        {
            done = false;
            progress = 0;
            progressBar.maxValue = files.Count;
            thread = new Thread(delegate ()
            {
                int x = 0;
                foreach(string st in files)
		        {
                    UnityEngine.Debug.Log("Converting " + x + "/" + files.Count);
                    progress = x;
                    ConvertFile(st);
                    x++;
                }
                done = true;
            });
            thread.Start();
        }


	}

    public void ConvertFile(string file)
    {
        string newPath = Path.GetDirectoryName(file);
        string newFile = Path.GetFileNameWithoutExtension(file);
        if(newFile.Substring(newFile.Length - 5) == ".stem")
        {
            newFile = newFile.Substring(0, newFile.Length - 5);
        }
        newFile += ".mp3";
        newPath += "/" + newFile;
        string command = " -i " + '"' + file + '"' + " " + '"' + newPath + '"';
        ProcessStartInfo processStart = new ProcessStartInfo("ffmpeg.exe");
        processStart.Arguments = command;
        if(!core.debugMode)
        { processStart.WindowStyle = ProcessWindowStyle.Hidden; }
        Process process = Process.Start(processStart);
        process.WaitForExit();
        process.Close();

		if(deleteAfterConversion)
		{
            File.Delete(file);
		}
    }

    
}
