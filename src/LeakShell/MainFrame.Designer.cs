namespace LeakShell
{
    partial class MainFrame
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.ColumnHeader chFilteredCount;
            System.Windows.Forms.ColumnHeader chFilteredSize;
            System.Windows.Forms.ColumnHeader chFilteredClassName;
            System.Windows.Forms.ColumnHeader chSnapshotCount;
            System.Windows.Forms.ColumnHeader chSnapshotSize;
            System.Windows.Forms.ColumnHeader chFiltered;
            System.Windows.Forms.ColumnHeader chReference;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainFrame));
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Title title1 = new System.Windows.Forms.DataVisualization.Charting.Title();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend2 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series3 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series4 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Title title2 = new System.Windows.Forms.DataVisualization.Charting.Title();
            this.btnAddSnapShot = new System.Windows.Forms.Button();
            this.tbResult = new System.Windows.Forms.TextBox();
            this.cbDontShowBCLTypes = new System.Windows.Forms.CheckBox();
            this.panelMain = new System.Windows.Forms.Panel();
            this.tcMain = new System.Windows.Forms.TabControl();
            this.tpList = new System.Windows.Forms.TabPage();
            this.scLists = new System.Windows.Forms.SplitContainer();
            this.lvCompare = new System.Windows.Forms.ListView();
            this.chCount = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chSize = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chClassName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ilMain = new System.Windows.Forms.ImageList(this.components);
            this.lvFiltered = new System.Windows.Forms.ListView();
            this.tpRaw = new System.Windows.Forms.TabPage();
            this.lvSnapshots = new System.Windows.Forms.ListView();
            this.chartSize = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.chartCount = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.btnClearSnapshots = new System.Windows.Forms.Button();
            this.llBlog = new System.Windows.Forms.LinkLabel();
            chFilteredCount = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            chFilteredSize = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            chFilteredClassName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            chSnapshotCount = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            chSnapshotSize = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            chFiltered = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            chReference = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.panelMain.SuspendLayout();
            this.tcMain.SuspendLayout();
            this.tpList.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scLists)).BeginInit();
            this.scLists.Panel1.SuspendLayout();
            this.scLists.Panel2.SuspendLayout();
            this.scLists.SuspendLayout();
            this.tpRaw.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chartSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chartCount)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // chFilteredCount
            // 
            chFilteredCount.Text = "Count";
            chFilteredCount.Width = 77;
            // 
            // chFilteredSize
            // 
            chFilteredSize.Text = "Size";
            chFilteredSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            chFilteredSize.Width = 136;
            // 
            // chFilteredClassName
            // 
            chFilteredClassName.Text = "Class Name";
            chFilteredClassName.Width = 534;
            // 
            // chSnapshotCount
            // 
            chSnapshotCount.Text = "Count";
            chSnapshotCount.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            chSnapshotCount.Width = 87;
            // 
            // chSnapshotSize
            // 
            chSnapshotSize.Text = "Size";
            chSnapshotSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            chSnapshotSize.Width = 125;
            // 
            // chFiltered
            // 
            chFiltered.Text = "";
            chFiltered.Width = 24;
            // 
            // chReference
            // 
            chReference.Text = "Ref";
            chReference.Width = 29;
            // 
            // btnAddSnapShot
            // 
            this.btnAddSnapShot.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAddSnapShot.Location = new System.Drawing.Point(16, 15);
            this.btnAddSnapShot.Name = "btnAddSnapShot";
            this.btnAddSnapShot.Size = new System.Drawing.Size(32, 102);
            this.btnAddSnapShot.TabIndex = 1;
            this.btnAddSnapShot.Text = "Set Ref";
            this.btnAddSnapShot.UseVisualStyleBackColor = true;
            this.btnAddSnapShot.Click += new System.EventHandler(this.btnSetReference_Click);
            // 
            // tbResult
            // 
            this.tbResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbResult.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbResult.Location = new System.Drawing.Point(0, 0);
            this.tbResult.MaxLength = 128000;
            this.tbResult.Multiline = true;
            this.tbResult.Name = "tbResult";
            this.tbResult.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbResult.Size = new System.Drawing.Size(745, 520);
            this.tbResult.TabIndex = 5;
            // 
            // cbDontShowBCLTypes
            // 
            this.cbDontShowBCLTypes.AutoSize = true;
            this.cbDontShowBCLTypes.Location = new System.Drawing.Point(18, 12);
            this.cbDontShowBCLTypes.Name = "cbDontShowBCLTypes";
            this.cbDontShowBCLTypes.Size = new System.Drawing.Size(195, 17);
            this.cbDontShowBCLTypes.TabIndex = 14;
            this.cbDontShowBCLTypes.Text = "Don\'t show BCL types and compare";
            this.cbDontShowBCLTypes.UseVisualStyleBackColor = true;
            this.cbDontShowBCLTypes.CheckedChanged += new System.EventHandler(this.cbDontShowBCLTypes_CheckedChanged);
            // 
            // panelMain
            // 
            this.panelMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelMain.Controls.Add(this.tcMain);
            this.panelMain.Location = new System.Drawing.Point(18, 35);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(753, 546);
            this.panelMain.TabIndex = 15;
            // 
            // tcMain
            // 
            this.tcMain.Alignment = System.Windows.Forms.TabAlignment.Bottom;
            this.tcMain.Controls.Add(this.tpList);
            this.tcMain.Controls.Add(this.tpRaw);
            this.tcMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tcMain.Location = new System.Drawing.Point(0, 0);
            this.tcMain.Margin = new System.Windows.Forms.Padding(0);
            this.tcMain.Name = "tcMain";
            this.tcMain.Padding = new System.Drawing.Point(0, 0);
            this.tcMain.SelectedIndex = 0;
            this.tcMain.Size = new System.Drawing.Size(753, 546);
            this.tcMain.TabIndex = 0;
            // 
            // tpList
            // 
            this.tpList.Controls.Add(this.scLists);
            this.tpList.Location = new System.Drawing.Point(4, 4);
            this.tpList.Margin = new System.Windows.Forms.Padding(0);
            this.tpList.Name = "tpList";
            this.tpList.Size = new System.Drawing.Size(745, 520);
            this.tpList.TabIndex = 1;
            this.tpList.Text = "Sorted Lists";
            this.tpList.UseVisualStyleBackColor = true;
            // 
            // scLists
            // 
            this.scLists.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scLists.Location = new System.Drawing.Point(0, 0);
            this.scLists.Name = "scLists";
            this.scLists.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // scLists.Panel1
            // 
            this.scLists.Panel1.Controls.Add(this.lvCompare);
            // 
            // scLists.Panel2
            // 
            this.scLists.Panel2.Controls.Add(this.lvFiltered);
            this.scLists.Size = new System.Drawing.Size(745, 520);
            this.scLists.SplitterDistance = 400;
            this.scLists.TabIndex = 0;
            // 
            // lvCompare
            // 
            this.lvCompare.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            chFiltered,
            this.chCount,
            this.chSize,
            this.chClassName});
            this.lvCompare.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvCompare.FullRowSelect = true;
            this.lvCompare.Location = new System.Drawing.Point(0, 0);
            this.lvCompare.Name = "lvCompare";
            this.lvCompare.Size = new System.Drawing.Size(745, 400);
            this.lvCompare.SmallImageList = this.ilMain;
            this.lvCompare.TabIndex = 0;
            this.lvCompare.UseCompatibleStateImageBehavior = false;
            this.lvCompare.View = System.Windows.Forms.View.Details;
            this.lvCompare.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.lvCompare_ColumnClick);
            this.lvCompare.DoubleClick += new System.EventHandler(this.lvCompare_DoubleClick);
            // 
            // chCount
            // 
            this.chCount.Text = "Count";
            this.chCount.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.chCount.Width = 77;
            // 
            // chSize
            // 
            this.chSize.Text = "Size";
            this.chSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.chSize.Width = 136;
            // 
            // chClassName
            // 
            this.chClassName.Text = "Class Name";
            this.chClassName.Width = 510;
            // 
            // ilMain
            // 
            this.ilMain.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ilMain.ImageStream")));
            this.ilMain.TransparentColor = System.Drawing.Color.Transparent;
            this.ilMain.Images.SetKeyName(0, "imgPin");
            this.ilMain.Images.SetKeyName(1, "imgPlay");
            this.ilMain.Images.SetKeyName(2, "imgCheck");
            this.ilMain.Images.SetKeyName(3, "imgReference");
            this.ilMain.Images.SetKeyName(4, "imgCurrent");
            // 
            // lvFiltered
            // 
            this.lvFiltered.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            chFilteredCount,
            chFilteredSize,
            chFilteredClassName});
            this.lvFiltered.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvFiltered.Location = new System.Drawing.Point(0, 0);
            this.lvFiltered.Name = "lvFiltered";
            this.lvFiltered.Size = new System.Drawing.Size(745, 116);
            this.lvFiltered.TabIndex = 1;
            this.lvFiltered.UseCompatibleStateImageBehavior = false;
            this.lvFiltered.View = System.Windows.Forms.View.Details;
            this.lvFiltered.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.lvFiltered_ColumnClick);
            this.lvFiltered.DoubleClick += new System.EventHandler(this.lvFiltered_DoubleClick);
            // 
            // tpRaw
            // 
            this.tpRaw.Controls.Add(this.tbResult);
            this.tpRaw.Location = new System.Drawing.Point(4, 4);
            this.tpRaw.Margin = new System.Windows.Forms.Padding(0);
            this.tpRaw.Name = "tpRaw";
            this.tpRaw.Size = new System.Drawing.Size(745, 520);
            this.tpRaw.TabIndex = 0;
            this.tpRaw.Text = "Raw";
            this.tpRaw.UseVisualStyleBackColor = true;
            // 
            // lvSnapshots
            // 
            this.lvSnapshots.AllowDrop = true;
            this.lvSnapshots.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lvSnapshots.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            chReference,
            chSnapshotCount,
            chSnapshotSize});
            this.lvSnapshots.FullRowSelect = true;
            this.lvSnapshots.Location = new System.Drawing.Point(54, 15);
            this.lvSnapshots.Name = "lvSnapshots";
            this.lvSnapshots.Size = new System.Drawing.Size(249, 102);
            this.lvSnapshots.SmallImageList = this.ilMain;
            this.lvSnapshots.TabIndex = 16;
            this.lvSnapshots.UseCompatibleStateImageBehavior = false;
            this.lvSnapshots.View = System.Windows.Forms.View.Details;
            this.lvSnapshots.DragDrop += new System.Windows.Forms.DragEventHandler(this.lvSnapshots_DragDrop);
            this.lvSnapshots.DragEnter += new System.Windows.Forms.DragEventHandler(this.lvSnapshots_DragEnter);
            this.lvSnapshots.DragOver += new System.Windows.Forms.DragEventHandler(this.lvSnapshots_DragOver);
            this.lvSnapshots.DoubleClick += new System.EventHandler(this.lvSnapshots_DoubleClick);
            this.lvSnapshots.KeyUp += new System.Windows.Forms.KeyEventHandler(this.lvSnapshots_KeyUp);
            // 
            // chartSize
            // 
            this.chartSize.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.chartSize.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.DiagonalRight;
            this.chartSize.BorderlineColor = System.Drawing.Color.Empty;
            this.chartSize.BorderlineWidth = 0;
            chartArea1.AxisX.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea1.AxisX.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            chartArea1.AxisX.TitleFont = new System.Drawing.Font("Verdana", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            chartArea1.AxisX2.TitleFont = new System.Drawing.Font("Verdana", 6.75F);
            chartArea1.AxisY.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea1.AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            chartArea1.AxisY.TitleFont = new System.Drawing.Font("Verdana", 6.75F);
            chartArea1.BackColor = System.Drawing.Color.Lavender;
            chartArea1.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.TopBottom;
            chartArea1.BackSecondaryColor = System.Drawing.Color.Transparent;
            chartArea1.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea1.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Solid;
            chartArea1.Name = "Default";
            chartArea1.ShadowColor = System.Drawing.Color.Transparent;
            this.chartSize.ChartAreas.Add(chartArea1);
            this.chartSize.Dock = System.Windows.Forms.DockStyle.Fill;
            legend1.BackColor = System.Drawing.Color.Transparent;
            legend1.Enabled = false;
            legend1.Font = new System.Drawing.Font("Trebuchet MS", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            legend1.IsTextAutoFit = false;
            legend1.Name = "Default";
            legend1.ShadowOffset = 1;
            this.chartSize.Legends.Add(legend1);
            this.chartSize.Location = new System.Drawing.Point(213, 3);
            this.chartSize.Name = "chartSize";
            series1.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(26)))), ((int)(((byte)(59)))), ((int)(((byte)(105)))));
            series1.BorderWidth = 2;
            series1.ChartArea = "Default";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series1.Color = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(65)))), ((int)(((byte)(140)))), ((int)(((byte)(240)))));
            series1.Legend = "Default";
            series1.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Diamond;
            series1.Name = "Series1";
            series1.ShadowColor = System.Drawing.Color.Black;
            series1.ShadowOffset = 2;
            series1.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Double;
            series1.YValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Double;
            series2.BorderColor = System.Drawing.Color.BlueViolet;
            series2.BorderWidth = 2;
            series2.ChartArea = "Default";
            series2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series2.Color = System.Drawing.Color.MediumOrchid;
            series2.Legend = "Default";
            series2.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Circle;
            series2.Name = "Series2";
            series2.ShadowColor = System.Drawing.Color.Black;
            series2.ShadowOffset = 2;
            series2.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Double;
            series2.YValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Double;
            this.chartSize.Series.Add(series1);
            this.chartSize.Series.Add(series2);
            this.chartSize.Size = new System.Drawing.Size(204, 102);
            this.chartSize.TabIndex = 17;
            title1.Name = "titleSize";
            title1.Text = "Total Size";
            title1.ToolTip = "Show the evolution of the total size of all objects";
            this.chartSize.Titles.Add(title1);
            // 
            // chartCount
            // 
            this.chartCount.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.chartCount.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.DiagonalRight;
            this.chartCount.BorderlineColor = System.Drawing.Color.Empty;
            this.chartCount.BorderlineWidth = 0;
            chartArea2.AxisX.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea2.AxisX.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            chartArea2.AxisX.TitleFont = new System.Drawing.Font("Verdana", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            chartArea2.AxisX2.TitleFont = new System.Drawing.Font("Verdana", 6.75F);
            chartArea2.AxisY.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea2.AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            chartArea2.AxisY.TitleFont = new System.Drawing.Font("Verdana", 6.75F);
            chartArea2.BackColor = System.Drawing.Color.Lavender;
            chartArea2.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.TopBottom;
            chartArea2.BackSecondaryColor = System.Drawing.Color.Transparent;
            chartArea2.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea2.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Solid;
            chartArea2.Name = "Default";
            chartArea2.ShadowColor = System.Drawing.Color.Transparent;
            this.chartCount.ChartAreas.Add(chartArea2);
            this.chartCount.Dock = System.Windows.Forms.DockStyle.Fill;
            legend2.BackColor = System.Drawing.Color.Transparent;
            legend2.Enabled = false;
            legend2.Font = new System.Drawing.Font("Trebuchet MS", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            legend2.IsTextAutoFit = false;
            legend2.Name = "Default";
            legend2.ShadowOffset = 1;
            this.chartCount.Legends.Add(legend2);
            this.chartCount.Location = new System.Drawing.Point(3, 3);
            this.chartCount.Name = "chartCount";
            series3.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(26)))), ((int)(((byte)(59)))), ((int)(((byte)(105)))));
            series3.BorderWidth = 2;
            series3.ChartArea = "Default";
            series3.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series3.Color = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(65)))), ((int)(((byte)(140)))), ((int)(((byte)(240)))));
            series3.Legend = "Default";
            series3.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Diamond;
            series3.Name = "Series1";
            series3.ShadowColor = System.Drawing.Color.Black;
            series3.ShadowOffset = 2;
            series3.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Double;
            series3.YValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Double;
            series4.BorderColor = System.Drawing.Color.BlueViolet;
            series4.BorderWidth = 2;
            series4.ChartArea = "Default";
            series4.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series4.Color = System.Drawing.Color.MediumOrchid;
            series4.Legend = "Default";
            series4.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Circle;
            series4.Name = "Series2";
            series4.ShadowColor = System.Drawing.Color.Black;
            series4.ShadowOffset = 2;
            series4.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Double;
            series4.YValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Double;
            this.chartCount.Series.Add(series3);
            this.chartCount.Series.Add(series4);
            this.chartCount.Size = new System.Drawing.Size(204, 102);
            this.chartCount.TabIndex = 17;
            title2.Name = "CountTitle";
            title2.Text = "Count";
            title2.ToolTip = "Show the evolution of objects count";
            this.chartCount.Titles.Add(title2);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.chartCount, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.chartSize, 1, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(354, 12);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(420, 108);
            this.tableLayoutPanel1.TabIndex = 18;
            // 
            // splitContainer1
            // 
            this.splitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(2);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.btnClearSnapshots);
            this.splitContainer1.Panel1.Controls.Add(this.btnAddSnapShot);
            this.splitContainer1.Panel1.Controls.Add(this.tableLayoutPanel1);
            this.splitContainer1.Panel1.Controls.Add(this.lvSnapshots);
            this.splitContainer1.Panel1.DoubleClick += new System.EventHandler(this.MainFrame_DoubleClick);
            this.splitContainer1.Panel1MinSize = 130;
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.llBlog);
            this.splitContainer1.Panel2.Controls.Add(this.panelMain);
            this.splitContainer1.Panel2.Controls.Add(this.cbDontShowBCLTypes);
            this.splitContainer1.Panel2.DoubleClick += new System.EventHandler(this.MainFrame_DoubleClick);
            this.splitContainer1.Size = new System.Drawing.Size(790, 752);
            this.splitContainer1.SplitterDistance = 132;
            this.splitContainer1.TabIndex = 19;
            // 
            // btnClearSnapshots
            // 
            this.btnClearSnapshots.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.btnClearSnapshots.Location = new System.Drawing.Point(309, 15);
            this.btnClearSnapshots.Name = "btnClearSnapshots";
            this.btnClearSnapshots.Size = new System.Drawing.Size(39, 102);
            this.btnClearSnapshots.TabIndex = 1;
            this.btnClearSnapshots.Text = "Clear";
            this.btnClearSnapshots.UseVisualStyleBackColor = true;
            this.btnClearSnapshots.Click += new System.EventHandler(this.btnClearSnapshots_Click);
            // 
            // llBlog
            // 
            this.llBlog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.llBlog.AutoSize = true;
            this.llBlog.LinkArea = new System.Windows.Forms.LinkArea(11, 18);
            this.llBlog.Location = new System.Drawing.Point(612, 589);
            this.llBlog.Name = "llBlog";
            this.llBlog.Size = new System.Drawing.Size(159, 17);
            this.llBlog.TabIndex = 16;
            this.llBlog.TabStop = true;
            this.llBlog.Text = "Written by Christophe Nasarre ";
            this.llBlog.UseCompatibleTextRendering = true;
            this.llBlog.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.llBlog_LinkClicked);
            // 
            // MainFrame
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(790, 752);
            this.Controls.Add(this.splitContainer1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainFrame";
            this.Text = "LeakShell";
            this.Load += new System.EventHandler(this.MainFrame_Load);
            this.DoubleClick += new System.EventHandler(this.MainFrame_DoubleClick);
            this.panelMain.ResumeLayout(false);
            this.tcMain.ResumeLayout(false);
            this.tpList.ResumeLayout(false);
            this.scLists.Panel1.ResumeLayout(false);
            this.scLists.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.scLists)).EndInit();
            this.scLists.ResumeLayout(false);
            this.tpRaw.ResumeLayout(false);
            this.tpRaw.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chartSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chartCount)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnAddSnapShot;
        private System.Windows.Forms.TextBox tbResult;
        private System.Windows.Forms.CheckBox cbDontShowBCLTypes;
        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.TabControl tcMain;
        private System.Windows.Forms.TabPage tpRaw;
        private System.Windows.Forms.TabPage tpList;
        private System.Windows.Forms.SplitContainer scLists;
        private System.Windows.Forms.ListView lvCompare;
        private System.Windows.Forms.ColumnHeader chCount;
        private System.Windows.Forms.ColumnHeader chSize;
        private System.Windows.Forms.ColumnHeader chClassName;
        private System.Windows.Forms.ListView lvFiltered;
        private System.Windows.Forms.ImageList ilMain;
        private System.Windows.Forms.ListView lvSnapshots;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartSize;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartCount;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button btnClearSnapshots;
        private System.Windows.Forms.LinkLabel llBlog;
    }
}

