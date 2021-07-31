using SoG.Modding.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoG.StatGrabber.Formatters
{
    public class LuaTableFormatter
    {
        private StringBuilder _builder = new StringBuilder(65535);

        public int IndentSize { get; set; } = 2;

        public string TableComment { get; set; } = "";

        public string TableName { get; set; } = "myTable";

        private int _currentIndent = 0;

        public string Format(IDictionary<object, object> table)
        {
            _builder.Length = 0;
            _currentIndent = 0;

            if (!string.IsNullOrEmpty(TableComment))
            {
                _builder.Append("--[[\n");

                foreach (var line in TableComment.Split('\n'))
                {
                    _builder.Append("--| " + line + "\n");
                }

                _builder.Append("--]]\n\n");
            }

            _builder.Append("local " + TableName + " = ");

            FormatObject(table);

            return _builder.ToString();
        }

        private void DoIndent()
        {
            _builder.Append(' ', IndentSize * _currentIndent);
        }

        private void FormatList(IList<object> list)
        {
            _builder.Append("{\n");

            _currentIndent++;

            if (list.Count > 0)
            {
                DoIndent();
            }

            bool didSomething = false;

            foreach (var item in list)
            {
                if (item == null)
                    continue;

                didSomething = true;

                FormatBasicObject(item);

                _builder.Append(", ");
            }

            _currentIndent--;

            if (didSomething)
            {
                // Removes dangling comma
                _builder.Length -= 2;
                _builder.Append('\n');
                DoIndent();
            }

            _builder.Append('}');
        }

        private void FormatTable(IDictionary<object, object> table)
        {
            _builder.Append("{\n");

            _currentIndent++;

            bool didSomething = false;

            foreach (var pair in table)
            {
                if (pair.Value == null)
                    continue;

                didSomething = true;

                DoIndent();

                _builder.Append('[');

                FormatBasicObject(pair.Key);

                _builder.Append("] = ");

                FormatObject(pair.Value);

                _builder.Append(",\n");
            }

            if (didSomething)
            {
                // Removes dangling comma
                _builder.Length -= 2;
                _builder.Append('\n');
            }

            _currentIndent--;

            DoIndent();

            _builder.Append('}');
        }

        private void FormatObject(object item)
        {
            if (item is IDictionary<object, object> convertedDict)
            {
                FormatTable(convertedDict);
            }
            else if (item is IList<object> convertedList)
            {
                FormatList(convertedList);
            }
            else
            {
                FormatBasicObject(item);
            }
        }

        private void FormatBasicObject(object item)
        {
            bool isString = item is string;

            if (isString)
                _builder.Append('\"');

            _builder.Append(item.ToString());

            if (isString)
                _builder.Append('\"');
        }
    }
}
