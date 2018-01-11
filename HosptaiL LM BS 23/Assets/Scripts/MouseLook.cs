using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using System.IO;
using UnityEngine;
using UnityEngine.VR;

[RequireComponent(typeof(AudioSource))]

public class MouseLook : MonoBehaviour
{
    //A boolean that flags whether there's a connected microphone  
    private bool micConnected = false;
    //The maximum and minimum available recording frequencies  
    private int minFreq;
    private int maxFreq;
    //A handle to the attached AudioSource  
    private AudioSource goAudioSource;
    AudioSource audio;
    AudioClip clip1;
    public float startTime = 0;
    string audioPath;
    public string filename;
    public GameObject MainCamera;
    public GameObject ReplayCamera;

    //variables to replay user and game status
    StreamWriter recordFile;              //the file the user input is recorded to
    StreamWriter recordFile2;
    public string recordFilename="DEFAULT";                   //the name of the file to record into
    public string recordFilenamePath;
    public string replayFilename;                   //the name of the replay file to load and replay
    public bool recordActive = false;               //TRUE when we are recording the users input etc
    public float recordStart = 0;                   //The time the recording started
    public float recordRate = 0.04f;                //The rate that records should be recorded
    private float nextRecord = 0.0f;                //Contains the time when the next record should be recorded
    private string[] replayCommands;                //the commands to replay
    public bool replayMode = false;                 //TRUE if a data file is being replayed
    public int replayIndex = 0;                     //Indicates which command to replay next

    public string objectName;
    public bool objectIN =false;
    public bool objectOUT = false;
    public bool actionIN = false;
    public bool actionOUT = false;
    public bool OnAction = false;

    void Awake()
    {
        replayMode = DataFileFound();
        if (replayMode)
        {
            VRSettings.LoadDeviceByName("");
            VRSettings.enabled = false;

            ReplayCamera.SetActive(true);
            MainCamera.SetActive(false);
            ReplayCamera.GetComponent<Camera>().fieldOfView = 60;
        }
        else
        {
            InputTracking.Recenter();
            ReplayCamera.SetActive(false);
            MainCamera.SetActive(true);

        }
    }




    // Use this for initialization
    void Start()
    {

        //see if we should replay a data file or let the user play the game
        if (DataFileFound())
        {
            replayMode = LoadDataFile();
            int start = replayFilename.IndexOf("20");
            filename = replayFilename.Substring(start, 19);
            clip1 = (AudioClip)Resources.Load(filename);
            audio = GetComponent<AudioSource>();
            audio.clip = clip1;
            audio.Play();
        }
        else
        {
            recordActive = true;
        }

        //Check if there is at least one microphone connected  
        if (Microphone.devices.Length <= 0)
        {
            //Throw a warning message at the console if there isn't  
            Debug.LogWarning("Microphone not connected!");
        }
        else if(recordActive) //At least one microphone is present  
        {
            //Set 'micConnected' to true  
            micConnected = true;

            //Get the default microphone recording capabilities  
            Microphone.GetDeviceCaps(null, out minFreq, out maxFreq);
            Debug.Log(maxFreq);
            //According to the documentation, if minFreq and maxFreq are zero, the microphone supports any frequency...  
            if (minFreq == 0 && maxFreq == 0)
            {
                //...meaning 44100 Hz can be used as the recording sampling rate  
                maxFreq = 44100;
            }

            //Get the attached AudioSource component  
            goAudioSource = this.GetComponent<AudioSource>();
            goAudioSource.clip = Microphone.Start(null, true, 1200, 20000);
            //StartCoroutine(Example());
        }

    }


    void OnApplicationQuit()
    {
        if (recordFile != null)
        {
            recordFile.Flush();
            recordFile.Close();

            recordFile2.Flush();
            recordFile2.Close();
        }

        Microphone.End(null); //Stop the audio recording  
        //SavWav.Save(recordFilename, goAudioSource.clip);
    }


    bool DataFileFound()
    {
        
        recordFilename =  DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xml";
        recordFilenamePath = Path.Combine("Assets/", recordFilename);
        string[] args = System.Environment.GetCommandLineArgs();
        if (args.Length == 2) replayFilename = args[1];
        if (System.IO.File.Exists(replayFilename))
            return true;
        else
            return false;
    }


    bool LoadDataFile()
    {
        replayCommands = File.ReadAllLines(replayFilename);
        return true;
    }

    // Update is called once per frame
    void Update()
    {
        
        if (replayMode && clip1)
           ReplayPose();
        else
            RecordPose();


    }





    void RecordPose()
    {
        if (recordActive)
        {
            if (recordStart == 0)
            {
                recordStart = Time.time;
                recordFile = new StreamWriter(recordFilenamePath, true);
                recordFile2 = new StreamWriter(recordFilenamePath+"action.xml", true);
            }
            if (Time.time > nextRecord)
            {
                nextRecord = Time.time + recordRate;
                Transform ct = Camera.main.transform;
                //recordFile.WriteLine("<event><time>" + (Time.time - recordStart).ToString() + "</time><pose><t>" + ct.localRotation.eulerAngles.x + "</t><u>" + ct.localRotation.eulerAngles.y + "</u><v>" + ct.localRotation.eulerAngles.z + "</v><w>" + ct.localRotation.w + "</w></pose><position><x>" + ct.position.x + "</x><y>" + ct.position.y + "</y><z>" + ct.position.z + "</z></poition></event>");

                if (objectIN && OnAction == false)
                {
                    objectIN = false;
                    recordFile.WriteLine("<event><time>" + (Time.time - recordStart).ToString() + "</time><objectIN>" + objectName + "</objectIN></event>");
                    recordFile2.WriteLine("<event><time>" + (Time.time - recordStart).ToString() + "</time><objectIN>" + objectName + "</objectIN></event>");
                }

                if (objectOUT && OnAction == false)
                {
                    objectOUT = false;
                    recordFile.WriteLine("<event><time>" + (Time.time - recordStart).ToString() + "</time><objectOUT>" + objectName + "</objectOUT></event>");
                    recordFile2.WriteLine("<event><time>" + (Time.time - recordStart).ToString() + "</time><objectOUT>" + objectName + "</objectOUT></event>");
                }

                if (actionIN)
                {
                    actionIN = false;
                    OnAction = true;
                    recordFile.WriteLine("<event><time>" + (Time.time - recordStart).ToString() + "</time><actionIN>" + objectName + "</actionIN></event>");
                    recordFile2.WriteLine("<event><time>" + (Time.time - recordStart).ToString() + "</time><actionIN>" + objectName + "</actionIN></event>");
                }

                if (actionOUT)
                {
                    actionOUT = false;
                    OnAction = false;
                    recordFile.WriteLine("<event><time>" + (Time.time - recordStart).ToString() + "</time><actionOUT>" + objectName + "</actionOUT></event>");
                    recordFile.WriteLine("<event><time>" + (Time.time - recordStart).ToString() + "</time><objectOUT>" + objectName + "</objectOUT></event>");
                    recordFile2.WriteLine("<event><time>" + (Time.time - recordStart).ToString() + "</time><actionOUT>" + objectName + "</actionOUT></event>");
                    recordFile2.WriteLine("<event><time>" + (Time.time - recordStart).ToString() + "</time><objectOUT>" + objectName + "</objectOUT></event>");
                }


            }

        }
    }


    void ReplayPose()
    {
        if (replayMode)
        {

            while (((Time.time - recordStart) > nextRecord) && (replayIndex < replayCommands.Length))
            {
                if (recordStart == 0)
                {
                    recordStart = Time.time;
                }

                if (replayCommands[replayIndex].Contains("<time>"))
                {
                    int timeStart = replayCommands[replayIndex].IndexOf("<time>") + 6;
                    int timeEnd = replayCommands[replayIndex].IndexOf("</time>");
                    float.TryParse(replayCommands[replayIndex].Substring(timeStart, timeEnd - timeStart), out nextRecord);
                }


                Transform ct = ReplayCamera.transform;
                if (replayIndex <= replayCommands.Length)
                {
                    if (replayCommands[replayIndex].Contains("<position>"))
                    {
                        float t, u, v, w, x, y, z;

                        int tStart = replayCommands[replayIndex].IndexOf("<t>") + 3;
                        int tEnd = replayCommands[replayIndex].IndexOf("</t>");
                        int uStart = replayCommands[replayIndex].IndexOf("<u>") + 3;
                        int uEnd = replayCommands[replayIndex].IndexOf("</u>");
                        int vStart = replayCommands[replayIndex].IndexOf("<v>") + 3;
                        int vEnd = replayCommands[replayIndex].IndexOf("</v>");
                        int wStart = replayCommands[replayIndex].IndexOf("<w>") + 3;
                        int wEnd = replayCommands[replayIndex].IndexOf("</w>");
                        int xStart = replayCommands[replayIndex].IndexOf("<x>") + 3;
                        int xEnd = replayCommands[replayIndex].IndexOf("</x>");
                        int yStart = replayCommands[replayIndex].IndexOf("<y>") + 3;
                        int yEnd = replayCommands[replayIndex].IndexOf("</y>");
                        int zStart = replayCommands[replayIndex].IndexOf("<z>") + 3;
                        int zEnd = replayCommands[replayIndex].IndexOf("</z>");

                        float.TryParse(replayCommands[replayIndex].Substring(tStart, tEnd - tStart), out t);
                        float.TryParse(replayCommands[replayIndex].Substring(uStart, uEnd - uStart), out u);
                        float.TryParse(replayCommands[replayIndex].Substring(vStart, vEnd - vStart), out v);
                        float.TryParse(replayCommands[replayIndex].Substring(wStart, wEnd - wStart), out w);
                        float.TryParse(replayCommands[replayIndex].Substring(xStart, xEnd - xStart), out x);
                        float.TryParse(replayCommands[replayIndex].Substring(yStart, yEnd - yStart), out y);
                        float.TryParse(replayCommands[replayIndex].Substring(zStart, zEnd - zStart), out z);

                        ReplayCamera.transform.position = new Vector3(x, y, z);
                        ReplayCamera.transform.rotation = Quaternion.Euler(t, u -90, v);
                        Debug.Log("<event><time>" + (Time.time - recordStart).ToString() + "</time><pose><t>" + t + "</t><u>" + u + "</u><v>" + v + "</v><w>" + w + "</w></pose><position><x>" + x + "</x><y>" + y + "</y><z>" + z + "</z></poition></event>");

                    }

                    replayIndex++;
                }
            }
        }
    }


    public static class SavWav
    {

        const int HEADER_SIZE = 44;

        public static bool Save(string filename, AudioClip clip)
        {
            /*if (!filename.ToLower().EndsWith(".wav"))
            {
                filename += ".wav";
            }

            var filepath = Path.Combine("Assets/", filename);

            Debug.Log(filepath);

            // Make sure directory exists if user is saving to sub dir.
            Directory.CreateDirectory(Path.GetDirectoryName(filepath));

            using (var fileStream = CreateEmpty(filepath))
            {

                ConvertAndWrite(fileStream, clip);

                WriteHeader(fileStream, clip);
            }
            */
            return true; // TODO: return false if there's a failure saving the file
        }

        public static AudioClip TrimSilence(AudioClip clip, float min)
        {
            var samples = new float[clip.samples];

            clip.GetData(samples, 0);

            return TrimSilence(new List<float>(samples), min, clip.channels, clip.frequency);
        }

        public static AudioClip TrimSilence(List<float> samples, float min, int channels, int hz)
        {
            return TrimSilence(samples, min, channels, hz, false, false);
        }

        public static AudioClip TrimSilence(List<float> samples, float min, int channels, int hz, bool _3D, bool stream)
        {
            int i;

            for (i = 0; i < samples.Count; i++)
            {
                if (Mathf.Abs(samples[i]) > min)
                {
                    break;
                }
            }

            samples.RemoveRange(0, i);

            for (i = samples.Count - 1; i > 0; i--)
            {
                if (Mathf.Abs(samples[i]) > min)
                {
                    break;
                }
            }

            samples.RemoveRange(i, samples.Count - i);

            var clip = AudioClip.Create("TempClip", samples.Count, channels, hz, _3D, stream);

            clip.SetData(samples.ToArray(), 0);

            return clip;
        }

        static FileStream CreateEmpty(string filepath)
        {
            var fileStream = new FileStream(filepath, FileMode.Create);
            byte emptyByte = new byte();

            for (int i = 0; i < HEADER_SIZE; i++) //preparing the header
            {
                fileStream.WriteByte(emptyByte);
            }

            return fileStream;
        }

        static void ConvertAndWrite(FileStream fileStream, AudioClip clip)
        {

            var samples = new float[clip.samples];

            clip.GetData(samples, 0);

            Int16[] intData = new Int16[samples.Length];
            //converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]

            Byte[] bytesData = new Byte[samples.Length * 2];
            //bytesData array is twice the size of
            //dataSource array because a float converted in Int16 is 2 bytes.

            int rescaleFactor = 32767; //to convert float to Int16

            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(samples[i] * rescaleFactor);
                Byte[] byteArr = new Byte[2];
                byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            fileStream.Write(bytesData, 0, bytesData.Length);
        }

        static void WriteHeader(FileStream fileStream, AudioClip clip)
        {

            var hz = clip.frequency;
            var channels = clip.channels;
            var samples = clip.samples;

            fileStream.Seek(0, SeekOrigin.Begin);

            Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
            fileStream.Write(riff, 0, 4);

            Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
            fileStream.Write(chunkSize, 0, 4);

            Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
            fileStream.Write(wave, 0, 4);

            Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
            fileStream.Write(fmt, 0, 4);

            Byte[] subChunk1 = BitConverter.GetBytes(16);
            fileStream.Write(subChunk1, 0, 4);

            UInt16 two = 2;
            UInt16 one = 1;

            Byte[] audioFormat = BitConverter.GetBytes(one);
            fileStream.Write(audioFormat, 0, 2);

            Byte[] numChannels = BitConverter.GetBytes(channels);
            fileStream.Write(numChannels, 0, 2);

            Byte[] sampleRate = BitConverter.GetBytes(hz);
            fileStream.Write(sampleRate, 0, 4);

            Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
            fileStream.Write(byteRate, 0, 4);

            UInt16 blockAlign = (ushort)(channels * 2);
            fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

            UInt16 bps = 16;
            Byte[] bitsPerSample = BitConverter.GetBytes(bps);
            fileStream.Write(bitsPerSample, 0, 2);

            Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
            fileStream.Write(datastring, 0, 4);

            Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
            fileStream.Write(subChunk2, 0, 4);

            //		fileStream.Close();
        }
    }
}
