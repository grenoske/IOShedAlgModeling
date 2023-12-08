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
        public int SeekTimeDirectional { get; set; } = 130;
        public int PositionTime { get; set; } = 10;
        public int RotationLatency { get; set; } = 8;
        public int TotalNumberOfRequests { get; set; } = 0;
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
            int trackSeekTime = Math.Abs(targetTrack - currentTrack) * PositionTime;

            if (targetTrack < currentTrack)
            {
                trackSeekTime += SeekTimeDirectional;
            }

            Console.WriteLine($"Seeking to track {targetTrack} takes {trackSeekTime} ms.");

            currentTrack = targetTrack;
        }

        public void Read(File file)
        {
            targetTrack = new Random().Next(0, Tracks.Count);
            Console.WriteLine($"Reading file with {file.NumberOfBlocks} blocks from the hard drive.");
            SeekToTrack(targetTrack);
        }

        public void Write(File file)
        {
            targetTrack = new Random().Next(0, Tracks.Count);
            Console.WriteLine($"Writing file with {file.NumberOfBlocks} blocks to the hard drive.");

            if (new Random().Next(0, 100) < 30)
            {
                Console.WriteLine("Organizing blocks in adjacent sectors.");
            }
        }
    }
    public struct Track
    {
        public Track(int sectorsPerTrack) { Sectors = sectorsPerTrack; }
        public int Sectors { get; set; }
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
    public class Process
    {
        private File file;
        public Process() { }
        public int RequestTime { get; set; } = 7;
        public void CreateRequest(HardDrive hardDrive)
        {
            if ( this.file == null || this.file.blocksRemaining == 0)
            {
                file = new File(GetRandomNumberOfBlocks());
            }

            // Визначення операції (запис або читання)
            var writeOperation = GetRandomOperation();

            if (writeOperation)
            {
                hardDrive.Write(file);
                file.blocksRemaining--;
            }
            else
            {
                hardDrive.Read(file);
                file.blocksRemaining--;
            }
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
