using System.Collections.Concurrent;

internal class Program
{
    private static async Task Main(string[] args)
    {
        await LogMessageAsync("Searching for files: MYE*.dmp", LogType.Info);
        string userProfileDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string pattern = "MYE*.dmp";


        CancellationTokenSource cts = new CancellationTokenSource();
        CancellationToken token = cts.Token;

        await Task.Run(async () =>
          {
              try
              {
                  var usersDirectory = Path.GetFullPath(Path.Combine(userProfileDirectory, @"..\"));
                  ConcurrentBag<string> files = new ConcurrentBag<string>();

                  await SearchFilesAsync(usersDirectory, pattern, files, token);
                  await LogMessageAsync($"{files.Count} files found", LogType.Info);

                  foreach (string item in files)
                  {
                      await Console.Out.WriteLineAsync(item);
                  }

                  DeleteFiles(files);

                  if (files.Count > 0) await LogMessageAsync($"Files deleted successfully!", LogType.SuccessfullOperation);
              }
              catch (Exception ex)
              {
                  await LogMessageAsync($"Unknown error occurred:{ex.Message} \n Help Link:{ex.HelpLink}", LogType.Error);
              }
          });

        Console.ReadLine();
    }

    private static async Task HandleException(Exception ex)
    {
        if (ex is UnauthorizedAccessException)
            await LogMessageAsync($"Access denied: {ex.Message}", LogType.Error);
        else if (ex is OperationCanceledException)
            await LogMessageAsync($"Search operation is canceled:{(ex as OperationCanceledException)?.CancellationToken}", LogType.Info);
        else
            await LogMessageAsync($"Unknown error occurred:{ex.Message} \n Help Link:{ex.HelpLink}", LogType.Error);
    }

    private static async Task SearchFilesAsync(string directory, string searchPattern, ConcurrentBag<string> results, CancellationToken cancellationToken)
    {
        try
        {
            string[] files = Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories);
            foreach (string item in files) results.Add(item);
        }
        catch (Exception ex)
        {
            await HandleException(ex);
        }


        try
        {
            string[] subDirectories = Directory.GetDirectories(directory);
            List<Task> tasks = new List<Task>();

            foreach (string subDirectory in subDirectories)
            {
                cancellationToken.ThrowIfCancellationRequested();
                tasks.Add(Task.Run(() => SearchFilesAsync(subDirectory, searchPattern, results, cancellationToken)));
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            await HandleException(ex);
        }
    }

    private static void DeleteFiles(ConcurrentBag<string> files)
    {
        foreach (string item in files)
        {
            if (File.Exists(item))
                File.Delete(item);
        }
    }

    private static async Task LogMessageAsync(string message, LogType type)
    {
        Console.ForegroundColor = type switch
        {
            LogType.Error => ConsoleColor.Red,
            LogType.Info => ConsoleColor.DarkYellow,
            LogType.SuccessfullOperation => ConsoleColor.Green,
        };

        await Console.Out.WriteLineAsync(message);
        Console.ResetColor();
    }
}

public enum LogType
{
    Error = 0,
    Info = 1,
    SuccessfullOperation = 2
}
