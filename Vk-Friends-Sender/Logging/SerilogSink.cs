using System;
using System.Linq;
using System.Text;

using Avalonia.Logging;
using Avalonia.Utilities;

namespace Vk_Friends_Sender.Logging {
	public class SerilogSink(LogEventLevel level, string[] areas) : ILogSink {
		public bool IsEnabled(LogEventLevel level1, string area) {
			return level <= level1 && areas.Contains(area);
		}

		public void Log(LogEventLevel pass_level, string area, object? source, string message_template) {
			Serilog.Log.Write((Serilog.Events.LogEventLevel)pass_level, Format(area, message_template, source, []));
		}

		public void Log(LogEventLevel pass_level, string area, object? source, string message_template, params object?[] property_values) {
			Serilog.Log.Write((Serilog.Events.LogEventLevel)pass_level, Format(area, message_template, source, property_values));
		}
		
		private static string Format(
                    string area,
                    string template,
                    object? source,
                    object?[] v)
                {
                    var result = new StringBuilder(template.Length);
                    var r = new CharacterReader(template.AsSpan());
                    var i = 0;
        
                    result.Append('[');
                    result.Append(area);
                    result.Append(']');
        
                    while (!r.End)
                    {
                        var c = r.Take();
        
                        if (c != '{')
                        {
                            result.Append(c);
                        }
                        else
                        {
                            if (r.Peek != '{')
                            {
                                result.Append('\'');
                                result.Append(i < v.Length ? v[i++] : null);
                                result.Append('\'');
                                r.TakeUntil('}');
                                r.Take();
                            }
                            else
                            {
                                result.Append('{');
                                r.Take();
                            }
                        }
                    }
        
                    if (source is object)
                    {
                        result.Append('(');
                        result.Append(source.GetType().Name);
                        result.Append(" #");
                        result.Append(source.GetHashCode());
                        result.Append(')');
                    }
        
                    return result.ToString();
                }
	}
}