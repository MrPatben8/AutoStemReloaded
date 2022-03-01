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
    public bool debugMode = false;
    public List<string> files;
    public string searchPath;
    public bool deleteAfterConversion;
    // Start is called before the first frame update
    void Start()
    {
        SelectDirectory();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SelectDirectory()
    {
        files = new List<string>();
        searchPath = StandaloneFileBrowser.OpenFolderPanel("Search In", "", false)[0];
        GetFiles();
        ConvertList();
        //RenderList();
    }

    void GetFiles()
    {
        files.Clear();
        files.AddRange(Directory.GetFiles(searchPath, "*.mp4", SearchOption.AllDirectories));
        files.AddRange(Directory.GetFiles(searchPath, "*.m4a", SearchOption.AllDirectories));
    }

    Thread thread;
    void ConvertList()
	{
        if(thread != null && thread.IsAlive)
        {
            thread.Abort();

        } else
        {
            thread = new Thread(delegate ()
            {
                int x = 0;
                foreach(string st in files)
		        {
                    x++;
                    UnityEngine.Debug.Log("Converting " + x + "/" + files.Count);
                    ConvertFile(st);
		        }
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
        if(!debugMode)
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
