using System.Diagnostics;
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
        public void AddRequest(File filePart, bool typeOfRequest, int targetTrack)
        {
            Console.WriteLine($"------------Requests.Count: {Requests.Count}");
            if (Requests.Count == QueueLength) // maximum number of request queue is 20 
            {
                driver.TimeForProcesQueue = 0;
                Console.WriteLine($"------------TotalReqNumber: {TotalRequestsNumber}");
                ProcessRequest();
                driver.TotalTime += driver.TimeForProcesQueue;
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
        public float TotalTime { get; set; } = 0;
        public int TotalQuantumTime { get; set; } = 20;
        public int CurrentQuantumTime { get; set; } = 0;
        public int CurrentTime { get; private set; } = 0;
        public int RequestPerSecond { get; set; } = 50;
        public Processor(int numberOfProcesses, int quantTime)
        {
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

        private void RecordTime(int TimeForProcesQueue, float koef, int requests)
        {

            if (TimeForProcesQueue < koef)
            {
                TotalTime += koef; // if queue is fulling slower
                                   // than driver write/read file, then 
                                   // driver wait till queue is full
            }
            TotalTime += TimeForProcesQueue;
            using (StreamWriter writer = new StreamWriter(".\\text.txt", true))
            {
                writer.WriteLine(TotalTime + ";" + requests);
            }
            Console.WriteLine($" Total time: {TimeForProcesQueue} ms");
        }

        private void CleanFile()
        {
            using (StreamWriter writer = new StreamWriter(".\\text.txt"))
            {
                writer.Write(string.Empty);
            }
        }

        public void ExecuteProcesses(HardDriveController hardDriveController)
        {
            float koef = 1000 / ((float)RequestPerSecond / (float)hardDriveController.QueueLength);
            while (hardDriveController.TotalRequestsNumber < 1000)
            {

                foreach (var process in Processes)
                {
                    
                    while (CurrentQuantumTime >= process.RequestTime)
                    {
                        process.CreateRequest(hardDriveController);
                        if (hardDriveController.Requests.Count == 20) 
                            RecordTime(hardDriveController.TimeForProcesQueue, koef, hardDriveController.TotalRequestsNumber);
                        CurrentQuantumTime -= process.RequestTime;
                    }
                    CurrentQuantumTime = TotalQuantumTime;


                }

                

            }
            Console.WriteLine($" Total time for 100000 requests: {TotalTime} ms");
        }
    }
    public class Process
    {
        private static int instanceCount = 0; // for rand seed
        private Random rand;
        private bool writeOperation = false;
        private File file;      
        public Process() { rand = new Random(instanceCount); instanceCount++; }
        public int RequestTime { get; set; } = 7;
        public int RequestPerSecondLimit { get; set; } = 1;
        public void CreateRequest(HardDriveController hardDriveCotroller)
        {
            if (this.file == null || this.file.BlocksRequestRemaining == 0)
            {
                file = new File(GetRandomNumberOfBlocks());

                // Визначення операції (запис або читання)
                writeOperation = GetRandomOperation();
            }

            if (writeOperation)
            {
                float percent = (float)file.BlocksRequestRemaining / (float)file.NumberOfBlocks;
                if (percent < 0.3)
                {
                    // driver can write up to 30% of file's blocks in 
                    // neighbour sector on same track
                    // targetTrack = targetTrack;
                    Console.WriteLine($"----Organizing blocks in adjacent sectors. CurrentPercent {percent} ");
                }
                else
                {
                    file.TargetTrack = rand.Next(0, hardDriveCotroller.NumberOfTracks);
                }
                hardDriveCotroller.AddRequest(file, writeOperation, file.TargetTrack);
                file.BlocksRequestRemaining--;
            }
            else
            {
                file.TargetTrack = rand.Next(0, hardDriveCotroller.NumberOfTracks);
                hardDriveCotroller.AddRequest(file, writeOperation, file.TargetTrack);
                file.BlocksRequestRemaining--;
            }
            //file.blocksRemaining--;
        }

        private int GetRandomNumberOfBlocks()
        {
            return rand.Next(1, 501);
            
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
        public int TargetTrack { get; set; }
        public int NumberOfBlocks { get; set; }
        public File(int numberOfBlocks)
        {
            blocksRemaining = numberOfBlocks;
            NumberOfBlocks = numberOfBlocks;
            BlocksRequestRemaining = numberOfBlocks;
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
            driveCController.TypeOfAlgorithm = 1;
            Processor singlePros = new Processor(numberOfProcesses:10, quantTime:20);
            singlePros.ExecuteProcesses(driveCController);
        }
    }
}
