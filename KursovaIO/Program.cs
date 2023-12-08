using System.Diagnostics;
using System.Security.AccessControl;

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
        private int currentTrack { get; set; } = 0;

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

            // disc feature. optim.
            // for example it is less time to move using moveing through last 130ms.
            // than from first to 500th straight it like 5000ms.
            // !need to some improvement
            if (Distance > (Tracks.Count / 2 + TimeToMoveFormFirstTrackToLast_opt / TimeToMoveOneTrack))
            {
                if (currentTrack < Tracks.Count / 2)
                {
                    SeekTime = (currentTrack - 0) * TimeToMoveOneTrack + TimeToMoveFormFirstTrackToLast_opt + (Tracks.Count - targetTrack) * TimeToMoveOneTrack;
                }
                else
                {
                    SeekTime = (Tracks.Count - currentTrack) * TimeToMoveOneTrack + TimeToMoveFormFirstTrackToLast_opt + (targetTrack - 0) * TimeToMoveOneTrack;
                }
            }
            else
            {
                SeekTime = Distance * TimeToMoveOneTrack;
            }

            PositionTime = SeekTime + RotationLatency;

            Console.WriteLine($"Seeking from {currentTrack} to track {targetTrack} takes {PositionTime} ms.");

            currentTrack = targetTrack;
        }

        public void Read(File file)
        {
            targetTrack = new Random().Next(0, Tracks.Count);
            Console.WriteLine($"Reading file with {file.NumberOfBlocks} blocks from the hard drive.(Current Bolock: {file.blocksRemaining}");
            SeekToTrack(targetTrack);
            TotalRequestsNumber++;
        }

        public void Write(File file)
        {
            
            Console.WriteLine($"Writing file with {file.NumberOfBlocks} blocks to the hard drive.(Current Bolock: {file.blocksRemaining}");
            float percent = (float)file.blocksRemaining / (float)file.NumberOfBlocks;
            if ( percent > 0.7)
            {
                // driver can write up to 30% of file's blocks in 
                // neighbour sector on same track
                // targetTrack = targetTrack;
                Console.WriteLine($"----Organizing blocks in adjacent sectors. CurrentPercent {percent} ");
            }
            else
            {
                targetTrack = new Random().Next(0, Tracks.Count);
            }
            SeekToTrack(targetTrack);
            TotalRequestsNumber++;
        }
    }
    public struct Track
    {
        public Track(int sectorsPerTrack) { Sectors = sectorsPerTrack; }
        public int Sectors { get; set; }
    }

    public class HardDriveController
    {
        public List<Request>? Requests { get; set; }
        public HardDrive driver { get; set; }
        public HardDriveController(HardDrive driver)
        {
            this.driver = driver;
        }
        public void AddRequest(File filePart, bool typeOfRequest)
        {
            if (Requests.Count == 20) // maximum number of request is 20 
            {
                ProcessRequest();
            }
            Requests.Add(new Request() { File = filePart, TypeOfRequest = typeOfRequest });
        }
        public void ProcessRequest()
        {

        }
    }

    public struct Request
    {
        public File File { get; set; }
        public bool TypeOfRequest { get; set; } // 0 - Read, 1 - Write
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

        public void ExecuteProcesses(HardDrive hardDrive)
        {
            while (hardDrive.TotalRequestsNumber < 1000)
            {
                foreach (var process in Processes)
                {
                    while (CurrentQuantumTime >= 7)
                    {
                        process.CreateRequest(hardDrive);
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
        public void CreateRequest(HardDrive hardDrive)
        {
            if (this.file == null || this.file.blocksRemaining == 0)
            {
                file = new File(GetRandomNumberOfBlocks());

                // Визначення операції (запис або читання)
                writeOperation = GetRandomOperation();
            }

            if (writeOperation)
            {
                hardDrive.Write(file);
            }
            else
            {
                hardDrive.Read(file);
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
            Processor singlePros = new Processor(numberOfProcesses:10, quantTime:20);
            singlePros.ExecuteProcesses(driveC);
        }
    }
}
