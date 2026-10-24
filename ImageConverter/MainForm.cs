using ImageMagick;

namespace ImageConverter;

public sealed class MainForm : Form
{
    private readonly ComboBox formatBox = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 150,
        AccessibleName = "変換先の画像形式"
    };
    private readonly Label dropLabel = new()
    {
        Text = "ここに画像ファイルを\nドラッグ＆ドロップ\nJPEG / PNG / BMP / GIF / TIFF / WebP / HEIC / AVIF",
        TextAlign = ContentAlignment.MiddleCenter,
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = Color.FromArgb(241, 246, 252),
        ForeColor = Color.FromArgb(35, 70, 110),
        AllowDrop = true,
        Margin = new Padding(0, 12, 0, 12)
    };
    private readonly Label statusLabel = new()
    {
        Text = "変換した画像は元のファイルと同じフォルダーに保存します。",
        AutoSize = true,
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 8, 0, 0)
    };
    private readonly ListView results = new()
    {
        View = View.Details,
        FullRowSelect = true,
        GridLines = true,
        Dock = DockStyle.Fill,
        AccessibleName = "変換結果"
    };
    private bool converting;

    public MainForm()
    {
        Text = "画像フォーマット変換";
        Font = new Font("Yu Gothic UI", 10F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(720, 480);
        MinimumSize = new Size(600, 440);
        StartPosition = FormStartPosition.CenterScreen;
        AllowDrop = true;

        formatBox.Items.AddRange(["JPEG (.jpg)", "PNG (.png)"]);
        formatBox.SelectedIndex = 0;
        var formatRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = Padding.Empty
        };
        formatRow.Controls.Add(new Label { Text = "変換先の形式", AutoSize = true, Margin = new Padding(0, 6, 12, 0) });
        formatRow.Controls.Add(formatBox);

        results.Columns.Add("元のファイル", 210);
        results.Columns.Add("結果", 100);
        results.Columns.Add("保存先 / 詳細", 350);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            ColumnCount = 1,
            RowCount = 5
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(formatRow, 0, 0);
        layout.Controls.Add(dropLabel, 0, 1);
        layout.Controls.Add(new Label
        {
            Text = "複数ファイルに対応 · 同じ形式はスキップ · 同名ファイルは連番で保存",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        }, 0, 2);
        layout.Controls.Add(results, 0, 3);
        layout.Controls.Add(statusLabel, 0, 4);
        Controls.Add(layout);

        // 子コントロール上へのドロップも同じ処理にする。
        EnableFileDrop(this);
        FormClosing += (_, e) =>
        {
            if (converting)
            {
                e.Cancel = true;
                statusLabel.Text = "変換中です。完了してから閉じてください。";
            }
        };
    }

    private void EnableFileDrop(Control control)
    {
        control.AllowDrop = true;
        control.DragEnter += (_, e) =>
            e.Effect = !converting && e.Data?.GetDataPresent(DataFormats.FileDrop) == true
                ? DragDropEffects.Copy : DragDropEffects.None;
        control.DragDrop += OnFilesDropped;
        foreach (Control child in control.Controls)
            EnableFileDrop(child);
    }

    private async void OnFilesDropped(object? sender, DragEventArgs e)
    {
        if (converting || e.Data?.GetData(DataFormats.FileDrop) is not string[] paths || paths.Length == 0)
            return;

        var format = formatBox.SelectedIndex == 0 ? OutputFormat.Jpeg : OutputFormat.Png;
        converting = true;
        formatBox.Enabled = false;
        results.Items.Clear();
        dropLabel.Text = "変換しています…";
        var converted = 0;
        var skipped = 0;
        var failed = 0;
        try
        {
            foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                statusLabel.Text = $"処理中: {Path.GetFileName(path)}";
                try
                {
                    var result = await Task.Run(() => ImageConversion.Convert(path, format));
                    if (result.Skipped) skipped++;
                    else converted++;
                    results.Items.Add(new ListViewItem([
                        Path.GetFileName(path),
                        result.Skipped ? "スキップ" : "変換完了",
                        result.OutputPath ?? "変換元と変換先が同じ形式です。"
                    ]));
                }
                catch (Exception exception)
                {
                    failed++;
                    var message = Directory.Exists(path) ? "フォルダーには対応していません。画像ファイルをドロップしてください。"
                        : exception is OutOfMemoryException or ArgumentException or MagickException
                            ? "読み込めない画像、または対応していないファイルです。"
                            : exception.Message;
                    results.Items.Add(new ListViewItem([Path.GetFileName(path), "エラー", message]));
                }
                results.Items[^1].EnsureVisible();
            }
            statusLabel.Text = $"完了: 変換 {converted} 件 / スキップ {skipped} 件 / エラー {failed} 件";
        }
        finally
        {
            converting = false;
            formatBox.Enabled = true;
            dropLabel.Text = "ここに画像ファイルを\nドラッグ＆ドロップ\nJPEG / PNG / BMP / GIF / TIFF / WebP / HEIC / AVIF";
        }
    }
}
