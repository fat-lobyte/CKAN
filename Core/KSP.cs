using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Transactions;
using log4net;

[assembly: InternalsVisibleTo("CKAN.Tests")]

namespace CKAN
{
    
    /// <summary>
    ///     Everything for dealing with KSP itself.
    /// </summary>
    public class KSP : IDisposable
    {
        public IUser User { get; set; }

        #region Fields and Properties

        private static readonly ILog log = LogManager.GetLogger(typeof(KSP));

        private readonly string gamedir;
        private KSPVersion version;

        public NetFileCache Cache { get; private set; }

        public RegistryManager RegistryManager
        {
            get { return RegistryManager.Instance(this); }
        }

        public Registry Registry
        {
            get { return RegistryManager.registry; }
        }

        #endregion
        #region Construction and Initialisation

        /// <summary>
        /// Returns a KSP object, insisting that directory contains a valid KSP install.
        /// Will initialise a CKAN instance in the KSP dir if it does not already exist.
        /// Throws a NotKSPDirKraken if directory is not a KSP install.
        /// </summary>
        public KSP(string directory, IUser user)
        {
            User = user;

            // Make sure our path is absolute and has normalised slashes.
            directory = KSPPathUtils.NormalizePath(Path.GetFullPath(directory));

            if (! IsKspDir(directory))
            {
                throw new NotKSPDirKraken(directory);
            }
            
            gamedir = directory;
            Init();
            Cache = new NetFileCache(DownloadCacheDir());
        }

        /// <summary>
        ///     Create the CKAN directory and any supporting files.
        /// </summary>
        private void Init()
        {
            log.DebugFormat("Initialising {0}", CkanDir());

            if (! Directory.Exists(CkanDir()))
            {
                User.RaiseMessage("Setting up CKAN for the first time...");
                User.RaiseMessage("Creating {0}", CkanDir());
                Directory.CreateDirectory(CkanDir());

                User.RaiseMessage("Scanning for installed mods...");
                ScanGameData();
            }

            if (! Directory.Exists(DownloadCacheDir()))
            {
                User.RaiseMessage("Creating {0}", DownloadCacheDir());
                Directory.CreateDirectory(DownloadCacheDir());
            }

            // Clear any temporary files we find. If the directory
            // doesn't exist, then no sweat; FilesystemTransaction
            // will auto-create it as needed.
            // Create our temporary directories, or clear them if they
            // already exist.
            if (Directory.Exists(TempDir()))
            {
                var directory = new DirectoryInfo(TempDir());
                foreach (FileInfo file in directory.GetFiles()) file.Delete();
                foreach (DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
            }

            log.DebugFormat("Initialised {0}", CkanDir());
        }

        /// <summary>
        /// Releases all resource used by the <see cref="CKAN.KSP"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="CKAN.KSP"/>. The <see cref="Dispose"/>
        /// method leaves the <see cref="CKAN.KSP"/> in an unusable state. After calling <see cref="Dispose"/>, you must
        /// release all references to the <see cref="CKAN.KSP"/> so the garbage collector can reclaim the memory that
        /// the <see cref="CKAN.KSP"/> was occupying.</remarks>
        public void Dispose()
        {
            if (Cache != null)
                Cache.Dispose();
        }

        #endregion

        #region KSP Directory Detection and Versioning

        /// <summary>
        /// Returns the path to our portable version of KSP if ckan.exe is in the same
        /// directory as the game. Otherwise, returns null.
        /// </summary>
        public static string PortableDir()
        {
            // Find the directory our executable is stored in.
            // In Perl, this is just `use FindBin qw($Bin);` Verbose enough, C#?
            string exe_dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            log.DebugFormat("Checking if KSP is in my exe dir: {0}", exe_dir);

            // Checking for a GameData directory probably isn't the best way to
            // detect KSP, but it works. More robust implementations welcome.
            if (IsKspDir(exe_dir))
            {
                log.InfoFormat("KSP found at {0}", exe_dir);
                return exe_dir;
            }

            return null;
        }

        /// <summary>
        /// Attempts to automatically find a KSP install on this system.
        /// Returns the path to the install on success.
        /// Throws a DirectoryNotFoundException on failure.
        /// </summary>
        public static string FindGameDir()
        {
            // See if we can find KSP as part of a Steam install.
            string ksp_steam_path = KSPPathUtils.KSPSteamPath();

            if (ksp_steam_path != null)
            {
                if (IsKspDir(ksp_steam_path))
                {
                    return ksp_steam_path;
                }

                log.DebugFormat("Have Steam, but KSP is not at \"{0}\".", ksp_steam_path);
            }

            // Oh noes! We can't find KSP!
            throw new DirectoryNotFoundException();
        }

        /// <summary>
        /// Checks if the specified directory looks like a KSP directory.
        /// Returns true if found, false if not.
        /// </summary>
        internal static bool IsKspDir(string directory)
        {
            //first we need to check is directory exists
            if (!Directory.Exists(Path.Combine(directory, "GameData")))
            {
                log.DebugFormat("Cannot find GameData in {0}", directory);
                return false;
            }
            
            if (!File.Exists(Path.Combine(directory, "readme.txt")))
            {
                log.DebugFormat("Cannot find readme in {0}", directory);
                return false;
            }

            //If both exist we should be able to get game version
            try
            {
                DetectVersion(directory);
            }
            catch (NotKSPDirKraken)
            {
                log.DebugFormat("Cannot detect KSP version in {0}", directory);
                return false;
            }
            log.DebugFormat("{0} looks like a GameDir", directory);
            return true;
        }


        /// <summary>
        /// Detects the version of KSP in a given directory.
        /// Throws a NotKSPDirKraken if anything goes wrong.
        /// </summary>
        private static KSPVersion DetectVersion(string directory)
        {
            //Contract.Requires<ArgumentNullException>(directory==null);

            string readme;
            try
            {
                // Slurp our README into memory
                readme = File.ReadAllText(Path.Combine(directory, "readme.txt"));
            }
            catch
            {
                log.Error("Could not open KSP readme.txt in "+directory);
                throw new NotKSPDirKraken("readme.txt not found or not readable");
            }

            // And find the KSP version. Easy! :)
            Match match = Regex.Match(readme, @"^Version\s+(\d+\.\d+\.\d+)",
                RegexOptions.IgnoreCase | RegexOptions.Multiline);

            if (match.Success)
            {
                string version = match.Groups[1].Value;
                log.DebugFormat("Found version {0}", version);
                return new KSPVersion(version);
            }

            // Oh noes! We couldn't find the version!
            log.Error("Could not find KSP version in readme.txt");

            throw new NotKSPDirKraken(directory, "Could not find KSP version in readme.txt");
        }
        
        /// <summary>
        /// Rebuilds the "Ships" directory inside the current KSP instance
        /// </summary>
        public void RebuildKSPSubDir()
        {
            string[] FoldersToCheck = { "Ships/VAB", "Ships/SPH", "Ships/@thumbs/VAB", "Ships/@thumbs/SPH" };
            foreach (string sRelativePath in FoldersToCheck)
            {
                string sAbsolutePath = ToAbsoluteGameDir(sRelativePath);
                if (!Directory.Exists(sAbsolutePath))
                    Directory.CreateDirectory(sAbsolutePath);
            }
        }

        #endregion

        #region Things which would be better as Properties

        public string GameDir()
        {
            return gamedir;
        }

        public string GameData()
        {
            return KSPPathUtils.NormalizePath(
                Path.Combine(GameDir(), "GameData")
            );
        }

        public string CkanDir()
        {
            return KSPPathUtils.NormalizePath(
                Path.Combine(GameDir(), "CKAN")
            );
        }

        public string DownloadCacheDir()
        {
            return KSPPathUtils.NormalizePath(
                Path.Combine(CkanDir(), "downloads")
            );
        }

        public string Ships()
        {
            return KSPPathUtils.NormalizePath(
                Path.Combine(GameDir(), "Ships")
            );
        }

        public string ShipsVab()
        {
            return KSPPathUtils.NormalizePath(
                Path.Combine(Ships(), "VAB")
            );
        }

        public string ShipsSph()
        {
            return KSPPathUtils.NormalizePath(
                Path.Combine(Ships(), "SPH")
            );
        }

        public string ShipsThumbs()
        {
            return KSPPathUtils.NormalizePath(
                Path.Combine(Ships(), "@thumbs")
            );
        }

        public string ShipsThumbsSPH()
        {
            return KSPPathUtils.NormalizePath(
                Path.Combine(ShipsThumbs(), "SPH")
            );
        }

        public string ShipsThumbsVAB()
        {
            return KSPPathUtils.NormalizePath(
                Path.Combine(ShipsThumbs(), "VAB")
            );
        }

        public string Tutorial()
        {
            return KSPPathUtils.NormalizePath(
                Path.Combine(GameDir(), "saves", "training")
            );
        }

        public string Scenarios()
        {
            return KSPPathUtils.NormalizePath(
                Path.Combine(GameDir(), "saves", "scenarios")
            );
        }

        public string TempDir()
        {
            return KSPPathUtils.NormalizePath(
                Path.Combine(CkanDir(), "temp")
            );
        }

        public KSPVersion Version()
        {
            if (version != null)
            {
                return version;
            }

            return version = DetectVersion(GameDir());
        }

        #endregion

        #region CKAN/GameData Directory Maintenance

        /// <summary>
        /// Removes all files from the download (cache) directory.
        /// </summary>
        public void CleanCache()
        {
            // TODO: We really should be asking our Cache object to do the
            // cleaning, rather than doing it ourselves.
            
            log.Debug("Cleaning cahce directory");

            string[] files = Directory.GetFiles(DownloadCacheDir(), "*", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                if (Directory.Exists(file))
                {
                    log.DebugFormat("Skipping directory: {0}", file);
                    continue;
                }

                log.DebugFormat("Deleting {0}", file);
                File.Delete(file);
            }
        }

        /// <summary>
        /// Clears the registry of DLL data, and refreshes it by scanning GameData.
        /// This operates as a transaction.
        /// This *saves* the registry upon completion.
        /// </summary>
        // TODO: This would likely be better in the Registry class itself.
        public void ScanGameData()
        {
            using (TransactionScope tx = CkanTransaction.CreateTransactionScope())
            {
                Registry.ClearDlls();

                // TODO: It would be great to optimise this to skip .git directories and the like.
                // Yes, I keep my GameData in git.

                // Alas, EnumerateFiles is *case-sensitive* in its pattern, which causes
                // DLL files to be missed under Linux; we have to pick .dll, .DLL, or scanning
                // GameData *twice*.
                //
                // The least evil is to walk it once, and filter it ourselves.
                IEnumerable<string> files = Directory.EnumerateFiles(
                                        GameData(),
                                        "*",
                                        SearchOption.AllDirectories
                                    );

                files = files.Where(file => Regex.IsMatch(file, @"\.dll$", RegexOptions.IgnoreCase));

                foreach (string dll in files.Select(KSPPathUtils.NormalizePath))
                {
                    Registry.RegisterDll(this, dll);
                }
                    
                tx.Complete();
            }
            RegistryManager.Save();
        }

        #endregion

        /// <summary>
        /// Returns path relative to this KSP's GameDir.
        /// </summary>
        public string ToRelativeGameDir(string path)
        {
            return KSPPathUtils.ToRelative(path, GameDir());
        }

        /// <summary>
        /// Given a path relative to this KSP's GameDir, returns the
        /// absolute path on the system. 
        /// </summary>
        public string ToAbsoluteGameDir(string path)
        {
            return KSPPathUtils.ToAbsolute(path, GameDir());
        }

        public override string ToString()
        {
            return "KSP Install:" + gamedir;
        }

        public override bool Equals(object obj)
        {
            var other = obj as KSP;
            return other != null ? gamedir.Equals(other.GameDir()) : base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return gamedir.GetHashCode();
        }
    }

}
