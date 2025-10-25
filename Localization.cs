using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BFKillFeedback
{
	public class Localization
	{
		public static Dictionary<SystemLanguage, Dictionary<string, string>> translations = new Dictionary<SystemLanguage, Dictionary<string, string>>();
		public static SystemLanguage language_current_using = SodaCraft.Localizations.LocalizationManager.CurrentLanguage;
		public static string Tr(string translation_key)
		{
			if (!translations.ContainsKey(language_current_using))
			{
				language_current_using = SystemLanguage.English;
			}
			if (!translations[language_current_using].ContainsKey(translation_key))
			{
				return "";
			}
			return translations[language_current_using][translation_key];
		}
		public static bool LoadLocalization(string localization_dir_path)
		{
			bool result = true;
			string mapping_file_path = Path.Combine(localization_dir_path, "enum_mapping.json");
			if (!File.Exists(mapping_file_path))
			{
				UnityEngine.Debug.LogError("BFKillFeedback: 本地化枚举映射表丢失/Missing localization enum mapping table");
				return false;
			}
			string mapping_table_content = File.ReadAllText(mapping_file_path);
			JObject? mapping_table_parsed = JsonConvert.DeserializeObject<JObject>(mapping_table_content);
			if (mapping_table_parsed == null)
			{
				UnityEngine.Debug.LogError("BFKillFeedback: 本地枚举映射表解析失败/Failed to deserialize localization enum mapping table");
				return false;
			}
			foreach (KeyValuePair<string, JToken?> language_mapped in mapping_table_parsed)
			{
				if (language_mapped.Value == null)
				{
					UnityEngine.Debug.LogError("BFKillFeedback: 获取本地化枚举映射表项目值时出错/Happend error on getting value of localization enum mapping table. Key=" + language_mapped.Key);
					result = false;
					continue;
				}
				if (Enum.TryParse<SystemLanguage>(language_mapped.Key, out SystemLanguage language_result))
				{
					string localization_file_path = Path.Combine(localization_dir_path, language_mapped.Value.ToString());
					if (!File.Exists(localization_file_path))
					{
						UnityEngine.Debug.LogError("BFKillFeedback: 本地化文件丢失/Missing localization file. Path=" + localization_file_path);
						result = false;
						continue;
					}
					string localization_file_content = File.ReadAllText(localization_file_path);
					JObject? localization_file_parsed = JsonConvert.DeserializeObject<JObject>(localization_file_content);
					if (localization_file_parsed == null)
					{
						UnityEngine.Debug.LogError("BFKillFeedback: 本地化文件解析失败/Failed to deserialize localization file. Path=" + localization_file_path);
						result = false;
						continue;
					}
					foreach (KeyValuePair<string, JToken?> translation_pairs in localization_file_parsed)
					{
						if (translation_pairs.Value == null)
						{
							UnityEngine.Debug.LogError("BFKillFeedback: 获取本地化文件项目值时出错/Happend error on getting value of localization file. Key=" + translation_pairs.Key);
							result = false;
							continue;
						}
						if (translation_pairs.Value.Type == JTokenType.String)
						{
							if (!translations.ContainsKey(language_result))
							{
								translations.Add(language_result, new Dictionary<string, string>());
							}
							translations[language_result].Add(translation_pairs.Key, translation_pairs.Value.ToString());
						}
						else
						{
							UnityEngine.Debug.LogError("BFKillFeedback: 本地化文件项目值类型出错/Get wrong type on getting value of localization file. Key=" + translation_pairs.Key);
							result = false;
							continue;
						}
					}
				}
			}
			return result;
		}
	}
}
