using System.Security.Cryptography;
using System;
using System.IO;
using System.Collections.Generic;

namespace HelloWold;

public class PasswordEntry
{
    public string Login { get; set; }
    public string Site { get; set; }
    public string Password { get; set; }
    public string Hash { get; set; }
}

public static class Program
{
    private static List<PasswordEntry> passwordEntries = new List<PasswordEntry>();
    private static string secretFolder;
    
    public static void Main()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appFolder = Path.Combine(appDataPath, "BulbaKey");
        
        string folderIdFile = Path.Combine(appFolder, "folder_id.txt");
        
        if (!Directory.Exists(appFolder))
            Directory.CreateDirectory(appFolder);
        
        string folderHash;
        if (File.Exists(folderIdFile))
        {
            folderHash = File.ReadAllText(folderIdFile).Trim();
        }
        else
        {
            folderHash = GenerateRandomHash();
            File.WriteAllText(folderIdFile, folderHash);
        }
        
        secretFolder = Path.Combine(appFolder, folderHash);
        
        if (!Directory.Exists(secretFolder))
            Directory.CreateDirectory(secretFolder);
        
        Console.WriteLine($"Folder: {secretFolder}");
        Console.WriteLine();
        
        LoadPasswords();
        
        Console.WriteLine("1) new password");
        Console.WriteLine("2) list passwords");
        Console.WriteLine("3) get password by key");
        
        switch (Console.ReadLine())
        {
            case "1":
                NewPassword();
                break;
            case "2":
                ListPasswords();
                break;
            case "3":
                GetPasswordByKey();
                break;
            default:
                Console.WriteLine("Invalid choice");
                break;
        }
    }
    
    private static string GenerateRandomHash()
    {
        string randomString = Guid.NewGuid().ToString() + DateTime.Now.Ticks.ToString();
        byte[] hashBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(randomString));
        return Convert.ToHexString(hashBytes).Substring(0, 32);
    }

    private static void NewPassword()
    {
        Console.Write("Login>> ");
        string login = Console.ReadLine();
        
        Console.Write("Site>> ");
        string site = Console.ReadLine();
        
        Console.Write("Password>> ");
        string password = Console.ReadLine();
        
        if (!string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(login))
        {
            byte[] hashBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password));
            string key = Convert.ToHexString(hashBytes);
            
            string fileName = GenerateRandomHash();
            string filePath = Path.Combine(secretFolder, fileName + ".txt");
            
            string content = $"{login}|{site}|{password}|{key}";
            
            char[] chars = content.ToCharArray();
            Array.Reverse(chars);
            string obfuscatedContent = new string(chars);
            
            File.WriteAllText(filePath, obfuscatedContent);
            
            var entry = new PasswordEntry
            {
                Login = login,
                Site = site,
                Password = password,
                Hash = key
            };
            
            passwordEntries.Add(entry);
            
            Console.WriteLine($"\nPassword saved!");
            Console.WriteLine($"SHA256 key: {key}");
            Console.WriteLine($"File name: {fileName}.txt");
            Console.WriteLine($"Site: {site}");
            Console.WriteLine($"Login: {login}");
        }
        else
        {
            Console.WriteLine("Login and password cannot be empty!");
        }
    }

    private static void ListPasswords()
    {
        if (passwordEntries.Count == 0)
        {
            Console.WriteLine("No saved passwords");
            return;
        }
        
        Console.WriteLine("\nSaved passwords:");
        Console.WriteLine(new string('-', 80));
        for (int i = 0; i < passwordEntries.Count; i++)
        {
            var entry = passwordEntries[i];
            Console.WriteLine($"{i + 1}) Site: {entry.Site}");
            Console.WriteLine($"   Login: {entry.Login}");
            Console.WriteLine($"   Key: {entry.Hash.Substring(0, 20)}...");
            Console.WriteLine(new string('-', 80));
        }
    }
    
    private static void GetPasswordByKey()
    {
        Console.Write("Enter SHA256 key>> ");
        string key = Console.ReadLine();
        
        if (string.IsNullOrEmpty(key))
        {
            Console.WriteLine("Key is empty");
            return;
        }
        
        string[] files = Directory.GetFiles(secretFolder, "*.txt");
        bool found = false;
        
        foreach (string file in files)
        {
            try
            {
                string content = File.ReadAllText(file);
                
                char[] chars = content.ToCharArray();
                Array.Reverse(chars);
                string decryptedContent = new string(chars);
                
                string[] parts = decryptedContent.Split('|');
                
                if (parts.Length == 4 && parts[3] == key)
                {
                    string login = parts[0];
                    string site = parts[1];
                    string password = parts[2];
                    
                    Console.WriteLine($"\nPassword found!");
                    Console.WriteLine($"Site: {site}");
                    Console.WriteLine($"Login: {login}");
                    Console.WriteLine($"Password: {password}");
                    Console.WriteLine($"File: {Path.GetFileName(file)}");
                    
                    found = true;
                    break;
                }
            }
            catch
            {
            }
        }
        
        if (!found)
        {
            Console.WriteLine("Password with this key not found");
        }
    }
    
    private static void LoadPasswords()
    {
        passwordEntries.Clear();
        if (!Directory.Exists(secretFolder))
            return;
            
        string[] files = Directory.GetFiles(secretFolder, "*.txt");
        
        foreach (string file in files)
        {
            try
            {
                string content = File.ReadAllText(file);
                
                char[] chars = content.ToCharArray();
                Array.Reverse(chars);
                string decryptedContent = new string(chars);
                
                string[] parts = decryptedContent.Split('|');
                
                if (parts.Length == 4)
                {
                    var entry = new PasswordEntry
                    {
                        Login = parts[0],
                        Site = parts[1],
                        Password = parts[2],
                        Hash = parts[3]
                    };
                    
                    passwordEntries.Add(entry);
                }
            }
            catch
            {
            }
        }
    }
}

