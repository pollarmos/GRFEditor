using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text.RegularExpressions;
using System.Threading;

namespace Utilities.Localization {
	/// <summary>Application UI language. File encodings and numeric cultures are independent.</summary>
	public static class UiText {
		private static readonly ResourceManager Resources = new ResourceManager("Utilities.Localization.UiStrings", typeof(UiText).Assembly);
		private static readonly Dictionary<string, string> Keys = LoadKeys();
		// Prefer specific multi-line templates over shorter templates with the same prefix.
		private static readonly KeyValuePair<string, string>[] MessageTemplates = Keys
			.Where(p => Regex.IsMatch(p.Key, @"\{\d+(?:[^{}]*)\}"))
			.OrderByDescending(p => Regex.Replace(p.Key, @"\{\d+(?:[^{}]*)\}", "").Length)
			.ToArray();
		private static readonly Dictionary<string, string> MessageCache = new Dictionary<string, string>();
		private static string _language = "en";
		private static readonly object Sync = new object();

		public static string Language => _language;

		public static void Initialize(string language) {
			if (language != "en" && language != "ko")
				throw new ArgumentException("Supported UI languages: en, ko.", nameof(language));
			lock (Sync) {
				_language = language;
				MessageCache.Clear();
			}
			// Do not change CurrentCulture: archive metadata and numeric parsing must stay stable.
			Thread.CurrentThread.CurrentUICulture = Culture;
			CultureInfo.DefaultThreadCurrentUICulture = Culture;
		}

		private static CultureInfo Culture => CultureInfo.GetCultureInfo(_language == "ko" ? "ko-KR" : "en");

		private static Dictionary<string, string> LoadKeys() {
			var result = new Dictionary<string, string>(StringComparer.Ordinal);
			foreach (DictionaryEntry item in Resources.GetResourceSet(CultureInfo.InvariantCulture, true, true))
				if (item.Value is string) result[(string)item.Value] = (string)item.Key;
			return result;
		}

		public static string GetResource(string key) {
			return Resources.GetString(key, Culture) ?? key;
		}

		public static string Get(string english) {
			if (english == null) return null;
			string key;
			return Keys.TryGetValue(english, out key) ? GetResource(key) : english;
		}

		public static string GetForLanguage(string english, string language) {
			if (language != "en" && language != "ko") throw new ArgumentException("Supported UI languages: en, ko.", nameof(language));
			string key;
			return Keys.TryGetValue(english, out key)
				? Resources.GetString(key, CultureInfo.GetCultureInfo(language)) ?? english : english;
		}

		public static string Filter(string filter) {
			if (String.IsNullOrEmpty(filter) || _language == "en") return filter;
			string[] parts = filter.Split('|');
			for (int i = 0; i + 1 < parts.Length; i += 2) {
				string translated = Get(parts[i]);
				if (translated == parts[i] && parts[i].EndsWith(" Files", StringComparison.Ordinal))
					translated = Get(parts[i].Substring(0, parts[i].Length - 6)) + " " + Get("Files");
				parts[i] = translated;
			}
			return String.Join("|", parts);
		}

		/// <summary>Localizes display messages while retaining original exception data for diagnostics.</summary>
		public static string Message(string message) {
			if (String.IsNullOrEmpty(message) || _language == "en") return message;
			string direct = Get(message);
			if (direct != message) return direct;
			if (message.Length > 32768) return message;
			lock (Sync) {
				string cached;
				if (MessageCache.TryGetValue(message, out cached)) return cached;
				foreach (var item in MessageTemplates) {
					var fields = new List<int>();
					int end = 0;
					string pattern = "\\A";
					foreach (Match field in Regex.Matches(item.Key, @"\{(\d+)(?:[^{}]*)\}")) {
						pattern += Regex.Escape(item.Key.Substring(end, field.Index - end)) + "([\\s\\S]*?)";
						fields.Add(Int32.Parse(field.Groups[1].Value, CultureInfo.InvariantCulture));
						end = field.Index + field.Length;
					}
					pattern += Regex.Escape(item.Key.Substring(end)) + "\\z";
					Match match;
					try { match = Regex.Match(message, pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(50)); }
					catch (RegexMatchTimeoutException) { continue; }
					if (!match.Success) continue;
					string localized = GetResource(item.Value);
					localized = Regex.Replace(localized, @"\{(\d+)(?:[^{}]*)\}", field => {
						int index = fields.IndexOf(Int32.Parse(field.Groups[1].Value, CultureInfo.InvariantCulture));
						return index >= 0 ? match.Groups[index + 1].Value : field.Value;
					});
					if (MessageCache.Count >= 256) MessageCache.Clear();
					MessageCache[message] = localized;
					return localized;
				}
			}
			// Display-only prefixes may be followed by a filename or other user data.
			foreach (var item in Keys) {
				if (item.Key.Length < 12 || !item.Key.EndsWith(" ", StringComparison.Ordinal)) continue;
				if (message.StartsWith(item.Key, StringComparison.Ordinal))
					return GetResource(item.Value) + message.Substring(item.Key.Length);
			}
			return message;
		}
	}
}
