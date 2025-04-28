using System;
using System.Diagnostics;
//https://jackma.com/2019/04/20/execute-a-bash-script-via-c-net-core/
public static class ShellHelper
  {
    public static Task<int> Bash(this string cmd)
    {
      var source = new TaskCompletionSource<int>();
      var escapedArgs = cmd.Replace("\"", "\\\"");
      var process = new Process
                      {
                        StartInfo = new ProcessStartInfo
                                      {
                                        FileName = "bash",
                                        Arguments = $"-c \"{escapedArgs}\"",
                                        RedirectStandardOutput = true,
                                        RedirectStandardError = true,
                                        UseShellExecute = false,
                                        CreateNoWindow = true
                                      },
                        EnableRaisingEvents = true
                      };
      process.Exited += (sender, args) =>
        {
          //logger.LogWarning(process.StandardError.ReadToEnd());
          //logger.LogInformation(process.StandardOutput.ReadToEnd());
          if (process.ExitCode == 0)
          {
            source.SetResult(0);
          }
          else
          {
            source.SetException(new Exception($"Command `{cmd}` failed with exit code `{process.ExitCode}`"));
          }

          process.Dispose();
        };

      try
      {
        process.Start();
      }
      catch (Exception e)
      {
        //logger.LogError(e, "Command {} failed", cmd);
        source.SetException(e);
      }
        process.WaitForExit();

      return source.Task;
    }
    public static Task<int> RegisterHttp(string prefix)
	{
		var source = new TaskCompletionSource<int>();
		//var source = new TaskCompletionSource<int>();
        string cmd = $"http add urlacl url={prefix} user={System.Security.Principal.WindowsIdentity.GetCurrent().Name}";
		var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "netsh",
				Arguments = cmd,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			},
			EnableRaisingEvents = true
		};
		process.Exited += (sender, args) =>
		{
			Console.WriteLine(process.StandardError.ReadToEnd());
			Console.WriteLine(process.StandardOutput.ReadToEnd());
			if (process.ExitCode == 0)
			{
				source.SetResult(0);
			}
			else
			{
				source.SetException(new Exception($"Command `{cmd}` failed with exit code `{process.ExitCode}`"));
			}

			process.Dispose();
		};

		try
		{
			process.Start();
		}
		catch (Exception e)
		{
            Console.WriteLine($"{cmd} has failed");
			//logger.LogError(e, "Command {} failed", cmd);
		}
		return source.Task;
	}
  }