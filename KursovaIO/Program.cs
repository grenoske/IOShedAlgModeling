﻿using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.AccessControl;
using System.Security.Cryptography;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace KursovaIO
{
    public class HardDrive
    {
        public HardDrive(int numberOfTracks, int sectorsPerTrack)
        {
            InitializeTracks(numberOfTracks, sectorsPerTrack);
        }

        public List<Track>? Tracks { get; set; }
        private int targetTrack;
        public int SeekTime { get; set; }
        public int TotalTime { get; set; }
        public int TimeForProcesQueue { get; set; }
        public int PositionTime { get; set; }
        public int TimeToMoveOneTrack { get; set; } = 10;
        public int TimeToMoveFormFirstTrackToLast_opt { get; set; } = 130;
        public int RotationLatency { get; set; } = 8;
        public int TotalRequestsNumber { get; set; } = 0;
        public int currentTrack { get; set; } = 0;

        private void InitializeTracks(int numberOfTracks, int sectorsPerTrack)
        {
            Tracks = new List<Track>();
            for (int i = 0; i < numberOfTracks; i++)
            {
                Tracks.Add(new Track(sectorsPerTrack));
            }
        }

        private void SeekToTrack(int targetTrack)
        {
            int Distance = Math.Abs(targetTrack - currentTrack);

            SeekTime = Distance * TimeToMoveOneTrack;

            PositionTime = SeekTime + RotationLatency;

            Console.WriteLine($"Seeking from {currentTrack} to track {targetTrack} takes {PositionTime} ms.");

            currentTrack = targetTrack;
            TimeForProcesQueue += PositionTime;
        }

        public void Read(File file, int targetTrack)
        {
            Console.WriteLine($"Reading file with {file.NumberOfBlocks} blocks from the hard drive.(Current Bolock: {file.blocksRemaining}");
            SeekToTrack(targetTrack);
            file.blocksRemaining--;
            TotalRequestsNumber++;
        }

        public void Write(File file, int targetTrack)
        {
            
            Console.WriteLine($"Writing file with {file.NumberOfBlocks} blocks to the hard drive.(Current Bolock: {file.blocksRemaining}");
            SeekToTrack(targetTrack);
            file.blocksRemaining--;
            TotalRequestsNumber++;
        }

        public void MoveToFirstTrack()
        {
            SeekToTrack(0);
        }
    }
    public struct Track
    {
        public Track(int sectorsPerTrack) { Sectors = sectorsPerTrack; }
        public int Sectors { get; set; }
    }

    public class HardDriveController
    {
        public int TypeOfAlgorithm { get; set; } = 1; // 1 - FCFS 2 - SSTF 3 - CircularLook
        public int QueueLength { get; set; } = 20;
        public List<Request>? Requests { get; set; }
        public HardDrive driver { get; set; }
        public int NumberOfTracks { get { return driver.Tracks.Count; } }
        public int TotalRequestsNumber { get { return driver.TotalRequestsNumber; } }
        public int TimeForProcesQueue { get { return driver.TimeForProcesQueue; } }
        public int TotalTime {  get { return driver.TotalTime; } }  
        public HardDriveController(HardDrive driver)
        {
            this.driver = driver;
            Requests = new List<Request>(); 
        }
        public void AddRequest(File filePart, bool typeOfRequest, int targetTrack, int time)
        {
            Requests.Add(new Request()
            {
                File = filePart,
                TypeOfRequest = typeOfRequest,
                targetTrack = targetTrack
            });

            if (time >= driver.TotalTime) // if proc work time higher than disc time
            {
                Console.WriteLine("==============procTime: " + time + " driverTime" + driver.TotalTime+"======disc start processing queue");
                driver.TimeForProcesQueue = 0;
                driver.TotalTime = time;
                ProcessRequest();
                driver.TotalTime += driver.TimeForProcesQueue;
            }

        }
        public void ProcessRequest()
        {
            switch(TypeOfAlgorithm) 
            {
                case 1: FCFS();break; 
                case 2: SSTF();break;
                case 3: CircularLook();break;
                default: Console.WriteLine("Err!not correctAlg!");break;
            }
            foreach (Request req in Requests)
            {
                if(req.TypeOfRequest == true)
                {
                    driver.Write(req.File, req.targetTrack);
                }
                else
                {
                    driver.Read(req.File, req.targetTrack);
                }

                if (req.MoveToFirstTrack)
                    driver.MoveToFirstTrack();
            }
            Requests.Clear();

        }

        public void FCFS()
        {
            // firstComeFirstserved
            // request is in correct order by default
            Requests = new List<Request>(Requests);
            // additional can add field DataTime 
            //
        }
        public void SSTF()
        {
            // sort by shortest seek time first
            List<Request> sortedRequests = new List<Request> { };
            int currentTrack = driver.currentTrack;
            while (Requests.Count != 0)
            {
                Request ClosestTrack = new Request() { targetTrack = -1};
                int minDistance = driver.Tracks.Count * 2;// just MAX number that is not be higher than any dist
                foreach (Request request in Requests)
                {
                    int Distance = Math.Abs(currentTrack - request.targetTrack);
                    if (Distance < minDistance)
                    {
                        ClosestTrack = request;
                        minDistance = Distance;
                    }
                    else if ((request.targetTrack == 0 || request.targetTrack == driver.Tracks.Count) 
                        && Distance == (driver.Tracks.Count - 1)) // from 0 to 500 seek time must be 13
                    {
                        ClosestTrack = request;
                        minDistance = driver.TimeToMoveFormFirstTrackToLast_opt / driver.TimeToMoveOneTrack;
                    }
                }
                if (ClosestTrack.targetTrack != -1)
                {
                    currentTrack = ClosestTrack.targetTrack;
                    sortedRequests.Add(ClosestTrack);
                    Requests.Remove(ClosestTrack);
                }
            }
            
            Requests = sortedRequests;
        }
        public void CircularLook()
        {
            // CircularLook
            List<Request> sortedRequests = new List<Request> { };
            while (Requests.Count != 0)
            {
                int CurrentHiNumberTrack = driver.currentTrack;
                foreach (Request request in Requests)
                {
                    if (request.targetTrack >= CurrentHiNumberTrack)
                    {
                        CurrentHiNumberTrack = request.targetTrack;
                        sortedRequests.Add(request);
                    }
                }
                Requests.RemoveAll(request => sortedRequests.Contains(request));
                // add a flag that need to move to first track
                Request req = sortedRequests[sortedRequests.Count - 1];
                req.MoveToFirstTrack = true;
                sortedRequests[sortedRequests.Count - 1] = req;
            }
            Requests = sortedRequests;

        }
    }


    public struct Request
    {
        public File File { get; set; }
        public bool MoveToFirstTrack { get; set; }
        public bool TypeOfRequest { get; set; } // 0 - Read, 1 - Write
        public int targetTrack { get; set; }
    }

    public class Processor
    {
        public int TotalTime { get; set; } = 0;
        public int ProcessesRequests { get; set; }
        public int TotalQuantumTime { get; set; } = 20;
        public int CurrentQuantumTime { get; set; } = 0;
        public int CurrentTime { get; private set; } = 0;
        public int TotalRequestNumberLimit { get; set; } = 1000;
        public int RequestPerSecond { get; set; }
        public int CurrentReqestPerSecond { get; set; }
        public Processor(int numberOfProcesses, int quantTime, int reqPerSec)
        {
            RequestPerSecond = reqPerSec;
            TotalQuantumTime = quantTime;
            CurrentQuantumTime = quantTime;
            InitializeProcesses(numberOfProcesses);
            CleanFile();
        }
        public List<Process>? Processes { get; set; }

        private void InitializeProcesses(int numberOfProcesses)
        {
            Processes = new List<Process>();
            int reqPerProc = CalculateRequestPerProcess(numberOfProcesses);
            for (int i = 0; i < numberOfProcesses; i++)
            {
                Processes.Add(new Process() { RequestPerSecondLimit = reqPerProc });
            }
        }

        private int CalculateRequestPerProcess(int numberOfProcesses)
        {
            return RequestPerSecond / numberOfProcesses;
        }

/*        private void RecordTime(int TimeForProcesQueue, float koef, int requests)
        {

            if (TimeForProcesQueue < koef)
            {
                TotalTime += koef; // if queue is fulling slower
                                   // than driver write/read file, then 
                                   // driver wait till queue is full
            }
            else
                TotalTime += TimeForProcesQueue;
            using (StreamWriter writer = new StreamWriter(".\\text.txt", true))
            {
                writer.WriteLine(TotalTime + ";" + requests);
            }
            Console.WriteLine($" Total time: {TimeForProcesQueue} ms");
        }*/

        private void CleanFile()
        {
            using (StreamWriter writer = new StreamWriter(".\\text.txt"))
            {
                writer.Write(string.Empty);
            }
        }

        public void ExecuteProcesses(HardDriveController hardDriveController, FileManager fileManager)
        {
            //float koef = 1000 / ((float)RequestPerSecond / (float)hardDriveController.QueueLength);
            while (hardDriveController.TotalRequestsNumber < TotalRequestNumberLimit)
            {
                CurrentReqestPerSecond = RequestPerSecond;
                foreach (var process in Processes)
                {
                    process.currRequestPerSecondLimit = process.RequestPerSecondLimit;
                }

                while (CurrentReqestPerSecond != 0 && hardDriveController.TotalRequestsNumber < TotalRequestNumberLimit)
                {

                    foreach (var process in Processes)
                    {

                        while (CurrentQuantumTime >= process.RequestTime)
                        {
                            process.CreateRequest(hardDriveController, fileManager);
                            TotalTime += process.RequestTime;
                            /*if (hardDriveController.Requests.Count == 20)
                                RecordTime(hardDriveController.TimeForProcesQueue, koef, hardDriveController.TotalRequestsNumber);*/
                            if (hardDriveController.TotalRequestsNumber < TotalRequestNumberLimit)
                                break;
                            CurrentQuantumTime -= process.RequestTime;
                            process.currRequestPerSecondLimit--;
                            CurrentReqestPerSecond--;

                        }
                        CurrentQuantumTime = TotalQuantumTime;
                    }
                    
                }

            }
            Console.WriteLine($" Total time for 100000 requests: {TotalTime} ms");
        }

        public void ExecuteProcesses2(HardDriveController hardDriveController, FileManager fileManager)
        {
            float koef = 1000 / ((float)RequestPerSecond / (float)hardDriveController.QueueLength);
            while (hardDriveController.TotalRequestsNumber < TotalRequestNumberLimit)
            {
                CurrentReqestPerSecond = RequestPerSecond;
                foreach (var process in Processes)
                {
                    process.currRequestPerSecondLimit = process.RequestPerSecondLimit;
                }

                while (CurrentReqestPerSecond != 0)
                {

                    foreach (var process in Processes)
                    {

                        while (CurrentQuantumTime >= process.RequestTime)
                        {
                            Console.WriteLine(hardDriveController.TotalRequestsNumber);
                            TotalTime += process.RequestTime;
                            UpdateTimeForProcesses(); 
                            process.CreateRequest(hardDriveController, fileManager);
                            ProcessesRequests++;
/*                            if (hardDriveController.Requests.Count == 20)
                                RecordTime(hardDriveController.TimeForProcesQueue, koef, hardDriveController.TotalRequestsNumber);*/


                            CurrentQuantumTime -= process.RequestTime;
                            process.currRequestPerSecondLimit--;
                            CurrentReqestPerSecond--;
                            if (CurrentReqestPerSecond == 0)
                                break;
                            if (ProcessesRequests == TotalRequestNumberLimit)
                            {
                                Console.WriteLine($" Total time for 100000 requests: {TotalTime} ms");
                                return;
                            }

                        }
                        CurrentQuantumTime = TotalQuantumTime;
                        if (CurrentReqestPerSecond == 0)
                            break;
                    }
                }
                TimeSync(hardDriveController.driver.TotalTime);
                if (TotalTime < ProcessesRequests / RequestPerSecond * 1000)
                    TotalTime = ProcessesRequests / RequestPerSecond * 1000;
                Console.WriteLine("----------TotalTIme +1000");
                Console.WriteLine($" Total time for 100000 requests: {TotalTime} ms");


            }
            
        }

        private void TimeSync (int driverTotalTime)
        {
            // sync time between proc and driver;
            if (driverTotalTime > TotalTime)
            {
               TotalTime = driverTotalTime;
            }
        }
        private void UpdateTimeForProcesses()
        {
            // set current time to processes
            foreach (var proc in Processes)
            {
                proc.CurrTime = TotalTime;
            }
        }
    }
    public class Process
    {
        private static int instanceCount = 0; // for rand seed
        private int nProc = 0;
        private Random rand;
        private bool writeOperation = false;
        private File file;
        public Process() { rand = new Random(instanceCount); nProc = instanceCount;  instanceCount++; }
        public int RequestTime { get; set; } = 7;
        public int CurrTime { get; set; }
        public int RequestPerSecondLimit { get; set; } = 1;
        public int currRequestPerSecondLimit { get; set; } = 1;
        public void CreateRequest(HardDriveController hardDriveCotroller, FileManager fM)
        {
            if (file == null || file.BlocksRequestRemaining == 0)
            {
                file = fM.Files[nProc];
                writeOperation = GetRandomOperation();
                file.BlocksRequestRemaining = file.Blocks.Length;
                file.blocksRemaining = file.Blocks.Length;
                Console.WriteLine($"file.blocksRemaining = file.Blocks.Length: {file.blocksRemaining}");
            }
            hardDriveCotroller.AddRequest(file, writeOperation, file.TargetTrack, CurrTime);   
            file.TargetTrack = file.Blocks[file.Blocks.Length - file.BlocksRequestRemaining] / hardDriveCotroller.driver.Tracks.Count;
            file.BlocksRequestRemaining--;
            if (file.BlocksRequestRemaining < 0 || file.blocksRemaining < 0)
                return;

        }
        private bool GetRandomOperation()
        {
            return rand.Next(0, 2) == 0;
        }

        public bool HasCompleted()
        {
            return file.IsCompleted();
        }
    }

    public class File
    {
        public int blocksRemaining = 0;
        public int BlocksRequestRemaining = 0;
        public int[] Blocks;
        public int TargetTrack { get; set; }
        public int NumberOfBlocks { get; set; }
        public File(int numberOfBlocks)
        {
            blocksRemaining = numberOfBlocks;
            NumberOfBlocks = numberOfBlocks;
            BlocksRequestRemaining = numberOfBlocks;
            Blocks = new int[numberOfBlocks];
        }

        public bool IsCompleted()
        {
            return blocksRemaining == 0;
        }
    }

    public class FileManager
    {
        private Random rand;
        private HardDrive drive;
        private enum FileType { SMALL, MEDIUM, LARGE }

        public List<File> Files;
        public FileManager(HardDrive drive) 
        {
            Files = new List<File>();
            rand = new Random(1);
            this.drive = drive;
        }
        public void CreateFiles(int filesNumber)
        {
            
            for (int i = 0; i < filesNumber; i++)
            {
                FileType type = (FileType)rand.Next(0, 3);
                Files.Add(new File(GetRandomNumberOfBlocks(type)));
            }
        }

        public void OrganizeFiles()
        {
            //
            int currSector = 1;
            int maxSectorNum = drive.Tracks.Count * drive.Tracks[0].Sectors;
            foreach (File file in Files)
            {
                for (int j = 0; j < file.NumberOfBlocks && currSector < maxSectorNum; currSector++)
                {
                    if (rand.NextDouble() <= 0.3)
                    {
                        file.Blocks[j] = currSector;
                        j++;
                    }
                }

            }
        }

        public void ShowStateOfDiscSpace()
        {
            char[][] Sectors = new char[500][];
            for (int i = 0; i < 500; i++)
            {
                Sectors[i] = new char[drive.Tracks[0].Sectors];
                // Ініціалізація кожного елемента нульовим значенням
                for (int j = 0; j < drive.Tracks[0].Sectors; j++)
                {
                    Sectors[i][j] = '0';
                }
            }
            foreach (File file in Files)
            {
                for (int i = 0; i < file.Blocks.Length; i++)
                {
                    int track = file.Blocks[i] / drive.Tracks[0].Sectors;
                    int sector = file.Blocks[i] % drive.Tracks[0].Sectors;
                    Sectors[track][sector] = '1';
                }
            }

            for (int i = 0; i < 500; i++)
            {
                for (int j = 0; j < drive.Tracks[0].Sectors; j++)
                {
                    Console.Write(Sectors[i][j]);
                }
                Console.WriteLine();
            }
        }

        private int GetRandomNumberOfBlocks(FileType fileType)
        {
            if (fileType == FileType.SMALL)
                return rand.Next(1, 11);
            else if (fileType == FileType.MEDIUM)
                return rand.Next(11, 151);
            else if (fileType == FileType.LARGE)
                return rand.Next(151, 501);
            else
                throw new Exception("No such file size");
        }

    }

    internal class Program
    {
        static void Main(string[] args)
        {
            HardDrive driveC = new HardDrive(numberOfTracks: 500, sectorsPerTrack: 100);
            FileManager fileManager = new FileManager(driveC);
            fileManager.CreateFiles(10);
            fileManager.OrganizeFiles();
            fileManager.ShowStateOfDiscSpace();

            
            HardDriveController driveCController = new HardDriveController(driveC);
            driveCController.TypeOfAlgorithm = 3;
            Processor singlePros = new Processor(numberOfProcesses: 10, quantTime: 20, reqPerSec: 50);
            singlePros.ExecuteProcesses2(driveCController, fileManager);

        }
    }
}
