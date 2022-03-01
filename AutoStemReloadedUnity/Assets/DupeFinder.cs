using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using SFB;
using UnityEngine.UI;
using System.Linq;

public class DupeFinder : MonoBehaviour
{
    public string searchPath;
    public List<string> files;

    public List<Track> duplicateTracks;

    public Text textDupesFound;
    public Text textIndivDupesFound;
    public Toggle ignoreCases;

    public GameObject template;
    public RectTransform contentArea;

    List<string> individualTracks = new List<string>();

    List<GameObject> listObjects = new List<GameObject>();

    public List<Track> tracks;
    [System.Serializable]
    public class Track
	{
        public string name;
        public string path;
        public string extension;
        public bool stem;
        public List<string> matches = new List<string>();
	}

    void Start()
    {

        template.SetActive(false);
    }

    public void SelectDirectory()
	{
        files = new List<string>();
        tracks = new List<Track>();
        individualTracks = new List<string>();
        searchPath = StandaloneFileBrowser.OpenFolderPanel("Search In", "", false)[0];
        GetFiles();
        FindDuplicates();
        HideNonDuplicates();
        RenderList();
    }

    void RenderList()
	{
        foreach(GameObject go in listObjects)
		{
            Destroy(go);
		}
        listObjects = new List<GameObject>();

        template.SetActive(true);
        contentArea.sizeDelta = new Vector2(0, 100 * duplicateTracks.Count);
        int x = 0;
		foreach(Track tk in duplicateTracks)
		{
            GameObject newItem = GameObject.Instantiate(template, contentArea.transform);
            newItem.transform.localPosition = new Vector2(0, -100 * x);

            newItem.GetComponent<Image>().color = x % 2 == 0 ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.3f, 0.3f, 0.3f);

            newItem.transform.GetChild(0).gameObject.SetActive(tk.stem);
            newItem.transform.GetChild(1).GetComponent<Text>().text = tk.name;
            newItem.transform.GetChild(2).GetComponent<Text>().text = Path.GetDirectoryName(tk.path);
            newItem.transform.GetChild(3).GetComponent<Text>().text = File.GetCreationTime(tk.path).ToShortDateString();
            float size = (new FileInfo(tk.path).Length / (1024 * 1024));
            newItem.transform.GetChild(4).GetComponent<Text>().text = "" + size + " Mb";

            newItem.transform.GetChild(5).GetComponent<Button>().onClick.AddListener(delegate
            {
                
                Debug.Log("DELETING: " + tk.path);
                File.Delete(tk.path);
                newItem.transform.GetChild(6).gameObject.SetActive(true);
            });

            newItem.transform.GetChild(7).GetComponent<Button>().onClick.AddListener(delegate
            {
                Application.OpenURL("file://" + tk.path);
            });

            newItem.transform.GetChild(8).GetComponent<Button>().onClick.AddListener(delegate
            {
                System.Diagnostics.Process.Start("explorer.exe", "/select," + tk.path);
            });

            listObjects.Add(newItem);

            if(!individualTracks.Contains(tk.name))
            {
                individualTracks.Add(tracks[x].name);
            }

            x++;
		}

        template.SetActive(false);
        textDupesFound.text = duplicateTracks.Count + " Duplicate tracks found";
        textIndivDupesFound.text = individualTracks.Count + " Individual tracks found";

    }

    void GetFiles()
	{
        files.Clear();
        files.AddRange(Directory.GetFiles(searchPath, "*.mp3", SearchOption.AllDirectories));
        files.AddRange(Directory.GetFiles(searchPath, "*.mp4", SearchOption.AllDirectories));
        files.AddRange(Directory.GetFiles(searchPath, "*.m4a", SearchOption.AllDirectories));

        tracks.Clear();
        foreach(string fl in files)
        {
            Track newTrack = new Track();
            newTrack.path = fl;
            newTrack.name = Path.GetFileNameWithoutExtension(fl);
            if(newTrack.name.Length > 5)
            {
                if(newTrack.name.Substring(newTrack.name.Length - 5) == ".stem")
                {
                    newTrack.name = newTrack.name.Substring(0, newTrack.name.Length - 5);
                    newTrack.stem = true;
                }
            }
            newTrack.extension = Path.GetExtension(fl);
            tracks.Add(newTrack);
        }
    }

    void FindDuplicates()
	{
        for(int x = 0; x<tracks.Count; x++)
		{
            foreach(Track tk in tracks)
			{
                if(ignoreCases.isOn)
                {
                    if(tracks[x].name.ToLower() == tk.name.ToLower() && tracks[x].path != tk.path)
                    {
                        tracks[x].matches.Add(tk.path);
                    }
				} else
				{
                    if(tracks[x].name == tk.name && tracks[x].path != tk.path)
                    {
                        tracks[x].matches.Add(tk.path);
                    }
                }
			}
        }
	}

    void HideNonDuplicates()
	{
        List<Track> unsortedDuplicateTracks = new List<Track>();
        foreach(Track tk in tracks)
        {
            if(tk.matches.Count > 0)
			{
                unsortedDuplicateTracks.Add(tk);
			}
		}
        duplicateTracks = unsortedDuplicateTracks.OrderBy(o => o.name).ToList();
        
	}
}
