using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.AccessControl;
using System.Security.Cryptography;

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
        }

        public void Read(File file, int targetTrack)
        {
            Console.WriteLine($"Reading file with {file.NumberOfBlocks} blocks from the hard drive.(Current Bolock: {file.blocksRemaining}");
            SeekToTrack(targetTrack);
            TotalRequestsNumber++;
        }

        public void Write(File file, int targetTrack)
        {
            
            Console.WriteLine($"Writing file with {file.NumberOfBlocks} blocks to the hard drive.(Current Bolock: {file.blocksRemaining}");
            SeekToTrack(targetTrack);
            TotalRequestsNumber++;
        }

        public void MoveToFirstTrack()
        {
            currentTrack = 0;
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
        public List<Request>? Requests { get; set; }
        public HardDrive driver { get; set; }
        public int NumberOfTracks { get { return driver.Tracks.Count; } }
        public int TotalRequestsNumber { get { return driver.TotalRequestsNumber; } }
        public HardDriveController(HardDrive driver)
        {
            this.driver = driver;
            Requests = new List<Request>(); 
        }
        public void AddRequest(File filePart, bool typeOfRequest, int targetTrack)
        {
            Console.WriteLine($"------------Requests.Count: {Requests.Count}");
            if (Requests.Count == 20) // maximum number of request is 20 
            {
                Console.WriteLine($"------------TotalReqNumber: {TotalRequestsNumber}");
                ProcessRequest();
            }
            Requests.Add(new Request() 
            { 
                File = filePart,
                TypeOfRequest = typeOfRequest,
                targetTrack = targetTrack
            });
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
                driver.MoveToFirstTrack();
            }
            Requests = sortedRequests;

        }
    }


    public struct Request
    {
        public File File { get; set; }
        public bool TypeOfRequest { get; set; } // 0 - Read, 1 - Write
        public int targetTrack { get; set; }
    }

    public class Processor
    {
        public int TotalQuantumTime { get; set; } = 20;
        public int CurrentQuantumTime { get; set; } = 0;
        public Processor(int numberOfProcesses, int quantTime)
        {
            TotalQuantumTime = quantTime;
            CurrentQuantumTime = quantTime;
            InitializeProcesses(numberOfProcesses);
        }
        public List<Process>? Processes { get; set; }

        private void InitializeProcesses(int numberOfProcesses)
        {
            Processes = new List<Process>();
            for (int i = 0; i < numberOfProcesses; i++)
            {
                Processes.Add(new Process());
            }
        }

        public void ExecuteProcesses(HardDriveController hardDriveController)
        {
            while (hardDriveController.TotalRequestsNumber < 1000)
            {
                foreach (var process in Processes)
                {
                    while (CurrentQuantumTime >= 7)
                    {
                        process.CreateRequest(hardDriveController);
                        CurrentQuantumTime -= process.RequestTime;
                    }
                    CurrentQuantumTime = TotalQuantumTime;
                }
            }
        }
    }
    public class Process
    {
        private bool writeOperation = false;
        private File file;
        public Process() { }
        public int RequestTime { get; set; } = 7;
        public void CreateRequest(HardDriveController hardDriveCotroller)
        {
            if (this.file == null || this.file.blocksRemaining == 0)
            {
                file = new File(GetRandomNumberOfBlocks());

                // Визначення операції (запис або читання)
                writeOperation = GetRandomOperation();
            }

            if (writeOperation)
            {
                float percent = (float)file.blocksRemaining / (float)file.NumberOfBlocks;
                if (percent < 0.3)
                {
                    // driver can write up to 30% of file's blocks in 
                    // neighbour sector on same track
                    // targetTrack = targetTrack;
                    Console.WriteLine($"----Organizing blocks in adjacent sectors. CurrentPercent {percent} ");
                }
                else
                {
                    file.TargetTrack = new Random().Next(0, hardDriveCotroller.NumberOfTracks);
                }
                hardDriveCotroller.AddRequest(file, writeOperation, file.TargetTrack);
            }
            else
            {
                file.TargetTrack = new Random().Next(0, hardDriveCotroller.NumberOfTracks);
                hardDriveCotroller.AddRequest(file, writeOperation, file.TargetTrack);
            }
            file.blocksRemaining--;
        }

        private int GetRandomNumberOfBlocks()
        {
            return new Random().Next(1, 501);
        }

        private bool GetRandomOperation()
        {
            return new Random().Next(0, 2) == 0;
        }

        public bool HasCompleted()
        {
            return file.IsCompleted();
        }
    }

    public class File
    {
        public int blocksRemaining = 0;
        public int TargetTrack { get; set; }
        public int NumberOfBlocks { get; set; }
        public File(int numberOfBlocks)
        {
            blocksRemaining = numberOfBlocks;
            NumberOfBlocks = numberOfBlocks;
        }

        public bool IsCompleted()
        {
            return blocksRemaining == 0;
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            HardDrive driveC = new HardDrive(numberOfTracks:500, sectorsPerTrack:100);
            HardDriveController driveCController = new HardDriveController(driveC);
            driveCController.TypeOfAlgorithm = 3;
            Processor singlePros = new Processor(numberOfProcesses:10, quantTime:20);
            singlePros.ExecuteProcesses(driveCController);
        }
    }
}
