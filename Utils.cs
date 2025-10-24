using FMOD;
using FMODUnity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace BFKillFeedback
{
    public class Utils
    {
        // 获取当前dll的所在目录
        public static string GetDllDirectory()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }
		public static bool LoadSoundWhiler(string dll_dir, string exe_dir, string file_name_prefix, string file_extension, ref List<Sound> target_list)
		{
			bool success = true;
			int counter = 0;
			while (true)
			{
				string path = "";
				if (File.Exists(Path.Combine(dll_dir, file_name_prefix + counter.ToString() + file_extension)))
				{ //检查dll目录
					path = Path.Combine(dll_dir, file_name_prefix + counter.ToString() + file_extension);
				}
				if (File.Exists(Path.Combine(exe_dir, file_name_prefix + counter.ToString() + file_extension)))
				{ //检查游戏流资源目录
					path = Path.Combine(exe_dir, file_name_prefix + counter.ToString() + file_extension);
				}
				if (path == "")
				{ //两边都没有目标文件，结束普通击杀音频的寻找
					break;
				}
				success = success && LoadSound(path, ref target_list);
				counter++;
			}
			return success;
		}
		public static bool LoadSound(string path_to_file, ref List<Sound> target_list)
		{
			bool success = true;
			RESULT fmod_create_result = RuntimeManager.CoreSystem.createSound(path_to_file, MODE.LOOP_OFF, out Sound sound);
			if (fmod_create_result == RESULT.OK)
			{
				target_list.Add(sound);
				UnityEngine.Debug.Log("BFKillFeedback: 成功加载音频 = " + path_to_file);
			}
			else
			{
				UnityEngine.Debug.LogError("BFKillFeedback: 加载音频时出错 = " + fmod_create_result.ToString());
				success = false;
			}
			return success;
		}
	}
}
