using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using KindredCommands.Services;

namespace KindredCommands.Models;

internal struct RecordKit(string name, int amount)
{
	public string Name { get; set; } = name; public int Amount { get; set; } = amount;
}

internal class DBKits
{
	private static readonly string FileDirectory = Path.Combine("BepInEx", "config", MyPluginInfo.PLUGIN_NAME);
	private static readonly string FileStartKits = "StartKits.json";
	private static readonly string FileUsedKits = "UsedKits.json";
	private static readonly string PathStarterKits = Path.Combine(FileDirectory, FileStartKits);
	private static readonly string PathUsedKits = Path.Combine(FileDirectory, FileUsedKits);

	public static ConcurrentDictionary<string, List<RecordKit>> StartKits = new();
	public static ConcurrentDictionary<ulong, bool> UsedKits = new();
	public static bool EnabledKitCommand = Core.ConfigSettings.KitHabilitado;
	public static string MessageAlreadyUsedKit = "";
	public static string MessageOnGivenKit = "";

	internal static void SaveData()
	{
		File.WriteAllText(PathStarterKits, JsonSerializer.Serialize(StartKits, new JsonSerializerOptions() { WriteIndented = true }));
		File.WriteAllText(PathUsedKits, JsonSerializer.Serialize(UsedKits, new JsonSerializerOptions() { WriteIndented = true }));
	}

	internal static void LoadKitsData()
	{
		if (!Directory.Exists(FileDirectory)) Directory.CreateDirectory(FileDirectory);
		LoadUsedKits();
		LoadStarterKit();
	}

	internal static void LoadUsedKits()
	{
		if (!File.Exists(PathUsedKits))
		{
			UsedKits.Clear();
			Core.Log.LogWarning("UsedKits DB Created.");
		}
		else
		{
			string json = File.ReadAllText(PathUsedKits);
			UsedKits = JsonSerializer.Deserialize<ConcurrentDictionary<ulong, bool>>(json);
			Core.Log.LogWarning("UsedKits DB Populated");
		}
	}

	internal static void LoadStarterKit()
	{
		if (!File.Exists(PathStarterKits))
		{
			StartKits.Clear();
			StartKits.TryAdd("startkit",
			[
				new RecordKit("Item_Boots_T09_Dracula_Brute", 1),
				new RecordKit("Item_Chest_T09_Dracula_Brute", 1),
				new RecordKit("Item_Gloves_T09_Dracula_Brute", 1),
				new RecordKit("Item_Legs_T09_Dracula_Brute", 1)
			]);
			Core.Log.LogWarning("StarterKit DB Created with default items.");
			SaveData();
		}
		else
		{
			try
			{
				string json = File.ReadAllText(PathStarterKits);
				var loadedKits = JsonSerializer.Deserialize<ConcurrentDictionary<string, List<RecordKit>>>(json);

				// If loaded kits is null or empty, add default kit
				if (loadedKits == null || loadedKits.IsEmpty)
				{
					loadedKits = new ConcurrentDictionary<string, List<RecordKit>>();
					loadedKits.TryAdd("startkit",
					[
						new RecordKit("Item_Boots_T09_Dracula_Brute", 1),
						new RecordKit("Item_Chest_T09_Dracula_Brute", 1),
						new RecordKit("Item_Gloves_T09_Dracula_Brute", 1),
						new RecordKit("Item_Legs_T09_Dracula_Brute", 1)
					]);
					Core.Log.LogWarning("Loaded StarterKit is empty. Created default kit.");
				}

				StartKits = loadedKits;
				Core.Log.LogWarning("StarterKit DB Populated");
			}
			catch (JsonException ex)
			{
				Core.Log.LogWarning($"Error loading StarterKit configuration: {ex.Message}");

				StartKits.Clear();
				StartKits.TryAdd("startkit",
				[
					new RecordKit("Item_Boots_T09_Dracula_Brute", 1),
					new RecordKit("Item_Chest_T09_Dracula_Brute", 1),
					new RecordKit("Item_Gloves_T09_Dracula_Brute", 1),
					new RecordKit("Item_Legs_T09_Dracula_Brute", 1)
				]);
				Core.Log.LogWarning("Fallback to default StarterKit due to configuration error.");
			}

			SaveData();
		}
	}	
}
