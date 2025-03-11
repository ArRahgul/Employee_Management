using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmdservice
{
    class Username
    {
        public static string GetUserName()
        {
            try
            {
                // Setup ProcessStartInfo
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c query user",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (Process process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    string username = ParseUsername(output);
                    //Console.WriteLine("Username: " + username);
                    return username;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }
        static string ParseUsername(string output)
        {
            // Split the output into lines
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Skip the header line and process each user line
            foreach (var line in lines.Skip(1))
            {
                // Check for lines that are not empty and process them
                if (!string.IsNullOrWhiteSpace(line))
                {
                    // The '>' character is used to denote the current logged in user
                    if (line.Trim().StartsWith(">"))
                    {
                        // Extract the username part
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0)
                        {
                            // Return the username, removing the '>' character
                            return parts[0].TrimStart('>');
                        }
                    }
                }
            }

            return "No active user found.";
        }
    }
}
