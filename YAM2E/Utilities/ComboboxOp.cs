using LAMP.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace LAMP;

public static class ComboboxOp
{
    /// <summary>
    /// Resizes the Flyout width of the combobox to fit the length of the longest item
    /// </summary>
    /// <param name="comboBox"></param>
    public static void AutoSize(this ComboBox comboBox)
    {
        const int scrollPadding = 20; //The scroll bar takes up space!

        if (comboBox == null) return;
        if (comboBox.Items.Count == 0) return;

        int maxWidth = 0;
        foreach (object item in comboBox.Items)
        {
            int temp = TextRenderer.MeasureText(item.ToString(), comboBox.Font).Width;
            if (temp > maxWidth) maxWidth = temp;
        }
        comboBox.DropDownWidth = maxWidth > comboBox.Width ? maxWidth + scrollPadding : comboBox.Width;
    }

    /// <summary>
    /// Adds the Contents of a IEnumerable with INamedResources as "Index - Name" to the list.
    /// </summary>
    public static void AddNumberedListContent(this ComboBox box, IEnumerable<INamedResource> sequence, string format = "X3")
    {
        box.BeginUpdate();
        for (int i = 0; i < sequence.Count(); i++)
        {
            INamedResource resource = sequence.ElementAt(i);
            string name = resource.Name?.Length > 0 ? $" - {resource.Name}" : "";
            box.Items.Add(i.ToString(format) + name);
        }
        box.EndUpdate();
    }
}
