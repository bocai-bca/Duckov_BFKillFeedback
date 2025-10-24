using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace BFKillFeedback
{
	public class KillTextHandler
	{
		public static int score_num_before_last_set = 0;
		public static int score_num_counter = 0; //当前的分数记分
		public static float score_num_last_set_time = 0;
		public static float last_switch_to_new_data_time = 0;
		public static float score_showup_time = 0; //分数文本的本次显示的开始时间
		public static float score_reach_num_time = 0; //分数文本到达数字的时间
		public static bool is_last_frame_score_reach = false; //上一帧是否有到达分数记分
		public static List<KillData> kill_datas = new List<KillData>();
		public static void Update(float time_now, RectTransform kill_rect, TextMeshProUGUI kill_text, RectTransform score_rect, TextMeshProUGUI score_text)
		{
			SetPosition(kill_rect, score_rect);
			SetScale(kill_rect, score_rect);
			//击杀文本
			float text_delta = time_now - last_switch_to_new_data_time;
			float text_alpha = 1.0f;
			if (ModBehaviour.text_fade_seconds != 0)
			{
				if (text_delta < ModBehaviour.text_stay_seconds)
				{
					text_alpha = ModBehaviour.text_color.a * Math.Clamp(text_delta / ModBehaviour.text_fade_seconds + 0.4f, 0.0f, 1.0f);
				}
				else if (kill_datas.Count > 0)
				{
					last_switch_to_new_data_time = time_now;
					kill_text.text = String.Format(ModBehaviour.text_template, new object[] { kill_datas[0].bekilleds_name, kill_datas[0].killed_this_count, kill_datas[0].use_weapon, (int)kill_datas[0].distance_meters, kill_datas[0].score_got });
					kill_datas.RemoveAt(0);
				}
				else
				{
					text_alpha = ModBehaviour.text_color.a * (1.0f - Math.Clamp((text_delta - ModBehaviour.text_fade_seconds - ModBehaviour.text_stay_seconds) / ModBehaviour.text_fade_seconds, 0.0f, 1.0f));
				}
			}
			if (ModBehaviour.disable_text)
			{
				text_alpha = 0.0f;
			}
			kill_text.color = new Color(ModBehaviour.text_color.r, ModBehaviour.text_color.g, ModBehaviour.text_color.b, text_alpha);
			//分数文本
			score_text.color = new Color(ModBehaviour.score_color.r, ModBehaviour.score_color.g, ModBehaviour.score_color.b, AlphaNow());
			score_text.text = ScoreNow().ToString();
			if (ScoreNow() == score_num_counter && !is_last_frame_score_reach)
			{
				score_reach_num_time = Time.time;
				is_last_frame_score_reach = true;
			}
		}
		public static void SetPosition(RectTransform kill_rect, RectTransform score_rect)
		{
			if (ModBehaviour.ui_transform == null)
			{
				return;
			}
			kill_rect.localPosition = new Vector3(ModBehaviour.text_position_offset.x * ModBehaviour.ui_transform.parent.position.x, ModBehaviour.text_position_offset.y * ModBehaviour.ui_transform.parent.position.y);
			score_rect.localPosition = new Vector3(ModBehaviour.score_text_position_offset.x * ModBehaviour.ui_transform.parent.position.x, ModBehaviour.score_text_position_offset.y * ModBehaviour.ui_transform.parent.position.y);
		}
		public static void SetScale(RectTransform kill_rect, RectTransform score_rect)
		{
			kill_rect.localScale = new Vector3(ModBehaviour.text_scale, ModBehaviour.text_scale);
			score_rect.localScale = new Vector3(ModBehaviour.score_scale, ModBehaviour.score_scale);
		}
		public static void NewKill(KillData kill_data)
		{
			if (kill_datas.Count < ModBehaviour.text_memory_length)
			{
				kill_datas.Add(kill_data);
			}
			AddScoreNum(kill_data.score_got);
		}
		public static void AddScoreNum(int num)
		{
			score_num_before_last_set = ScoreNow();
			if (AlphaNow() == 0.0f)
			{
				ScoreWakeup();
			}
			score_num_counter += num;
			score_num_last_set_time = Time.time;
			is_last_frame_score_reach = false;
		}
		public static void ScoreWakeup()
		{
			score_num_counter = 0;
			score_showup_time = Time.time;
			score_reach_num_time = Time.time + 114514.0f;
		}
		// 获取当前的分数
		public static int ScoreNow()
		{
			float delta_last_set = Time.time - score_num_last_set_time; //自上次设定分数过去的秒数
			return Math.Clamp(score_num_before_last_set + (int)Math.Clamp(delta_last_set * ModBehaviour.score_text_number_increase_per_second, 0.0f, float.MaxValue), 0, score_num_counter);
		}
		// 获取当前分数的不透明度
		public static float AlphaNow()
		{
			if (ModBehaviour.disable_score)
			{
				return 0.0f;
			}
			if (ScoreNow() == score_num_counter)
			{
				// 熄灭alpha
				float delta_reach = Time.time - score_reach_num_time - ModBehaviour.score_text_stay_seconds;
				float fadeout_alpha = ModBehaviour.score_color.a * (1.0f - Math.Clamp(delta_reach / ModBehaviour.text_fade_seconds, 0.0f, 1.0f));
				return fadeout_alpha;
			}
			else
			{
				// 亮起alpha
				float delta_showup = Time.time - score_showup_time;
				float wakeup_alpha = ModBehaviour.score_color.a * Math.Clamp(delta_showup / ModBehaviour.text_fade_seconds, 0.0f, 1.0f);
				return wakeup_alpha;
			}
		}
		public struct KillData
		{
			public string bekilleds_name;
			public string use_weapon;
			public int score_got;
			public float distance_meters;
			public int killed_this_count; //该种敌人被击杀的数量(含这一个)
			public KillData(string new_name, string new_weapon, int new_score, float new_distance, int new_killed_count)
			{
				bekilleds_name = new_name;
				use_weapon = new_weapon;
				score_got = new_score;
				distance_meters = new_distance;
				killed_this_count = new_killed_count;
			}
		}
	}
}
