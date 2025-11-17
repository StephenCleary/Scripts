<Query Kind="Statements" />

var text = System.Windows.Clipboard.GetText();
var singleLineEndings = new Regex(@"(?<!\r\n)\r\n(?!\r\n)");
text = singleLineEndings.Replace(text, " ");
text = text.Replace("\r\n\r\n", "\r\n");
System.Windows.Clipboard.SetText(text);

/*
Change text enclosed in underscores to italic:

Press Ctrl+Home to move to the start of the document.
Press Ctrl+H to activate the Replace dialog.
Click More >> to expand the dialog.
Tick the 'Use wildcards' check box.
Click in the 'Find what' box.
Enter   _(*)_
Click in the 'Replace with' box.
Enter   \1
Select Format > Font...
Select 'Italic' in the 'Font Style' list box and click OK.
Click 'Replace All'.
*/
