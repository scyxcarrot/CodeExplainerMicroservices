namespace IDS.Amace.GUI
{
    partial class CupPanel
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pnlDimensions = new System.Windows.Forms.GroupBox();
            this.cmbCupType = new System.Windows.Forms.ComboBox();
            this.updApertureAngle = new System.Windows.Forms.NumericUpDown();
            this.lblAperture = new System.Windows.Forms.Label();
            this.updInnerDiameter = new System.Windows.Forms.NumericUpDown();
            this.lblCupType = new System.Windows.Forms.Label();
            this.lblDiameter = new System.Windows.Forms.Label();
            this.pnlOrientation = new System.Windows.Forms.GroupBox();
            this.updInclination = new System.Windows.Forms.NumericUpDown();
            this.lblInclination = new System.Windows.Forms.Label();
            this.updAnteversion = new System.Windows.Forms.NumericUpDown();
            this.lblAnteversion = new System.Windows.Forms.Label();
            this.pnlPosition = new System.Windows.Forms.GroupBox();
            this.btnPos = new System.Windows.Forms.Button();
            this.btnAnt = new System.Windows.Forms.Button();
            this.btnLat = new System.Windows.Forms.Button();
            this.btnMed = new System.Windows.Forms.Button();
            this.btnInf = new System.Windows.Forms.Button();
            this.btnSup = new System.Windows.Forms.Button();
            this.lblAntPosUnit = new System.Windows.Forms.Label();
            this.lblMedLatUnit = new System.Windows.Forms.Label();
            this.lblSupInfUnit = new System.Windows.Forms.Label();
            this.lblReference = new System.Windows.Forms.Label();
            this.refPointList = new System.Windows.Forms.ComboBox();
            this.txtSupInf = new System.Windows.Forms.TextBox();
            this.txtAntPos = new System.Windows.Forms.TextBox();
            this.txtMedLat = new System.Windows.Forms.TextBox();
            this.pnlRbv = new System.Windows.Forms.GroupBox();
            this.chkRbvPreview = new System.Windows.Forms.CheckBox();
            this.pnlVisualisation = new System.Windows.Forms.GroupBox();
            this.btnCupView = new System.Windows.Forms.Button();
            this.btnInclinationView = new System.Windows.Forms.Button();
            this.btnAnteversionView = new System.Windows.Forms.Button();
            this.chkCupVector = new System.Windows.Forms.CheckBox();
            this.chkInclination = new System.Windows.Forms.CheckBox();
            this.chkAnteversion = new System.Windows.Forms.CheckBox();
            this.pnlFlanges = new System.Windows.Forms.GroupBox();
            this.updFlangeThickness = new System.Windows.Forms.NumericUpDown();
            this.lblFlangeThickness = new System.Windows.Forms.Label();
            this.lblTitle = new System.Windows.Forms.Label();
            this.pnlDimensions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.updApertureAngle)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.updInnerDiameter)).BeginInit();
            this.pnlOrientation.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.updInclination)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.updAnteversion)).BeginInit();
            this.pnlPosition.SuspendLayout();
            this.pnlRbv.SuspendLayout();
            this.pnlVisualisation.SuspendLayout();
            this.pnlFlanges.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.updFlangeThickness)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlDimensions
            // 
            this.pnlDimensions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlDimensions.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlDimensions.Controls.Add(this.cmbCupType);
            this.pnlDimensions.Controls.Add(this.updApertureAngle);
            this.pnlDimensions.Controls.Add(this.lblAperture);
            this.pnlDimensions.Controls.Add(this.updInnerDiameter);
            this.pnlDimensions.Controls.Add(this.lblCupType);
            this.pnlDimensions.Controls.Add(this.lblDiameter);
            this.pnlDimensions.Location = new System.Drawing.Point(0, 46);
            this.pnlDimensions.Name = "pnlDimensions";
            this.pnlDimensions.Size = new System.Drawing.Size(225, 103);
            this.pnlDimensions.TabIndex = 15;
            this.pnlDimensions.TabStop = false;
            this.pnlDimensions.Text = "Dimensions";
            // 
            // cmbCupType
            // 
            this.cmbCupType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbCupType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCupType.FormattingEnabled = true;
            this.cmbCupType.Location = new System.Drawing.Point(126, 45);
            this.cmbCupType.Name = "cmbCupType";
            this.cmbCupType.Size = new System.Drawing.Size(66, 21);
            this.cmbCupType.TabIndex = 1;
            this.cmbCupType.SelectedIndexChanged += new System.EventHandler(this.OnCupTypeValueChanged);
            // 
            // updApertureAngle
            // 
            this.updApertureAngle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.updApertureAngle.Location = new System.Drawing.Point(126, 71);
            this.updApertureAngle.Maximum = new decimal(new int[] {
            360,
            0,
            0,
            0});
            this.updApertureAngle.Name = "updApertureAngle";
            this.updApertureAngle.Size = new System.Drawing.Size(65, 20);
            this.updApertureAngle.TabIndex = 2;
            this.updApertureAngle.ValueChanged += new System.EventHandler(this.OnApertureAngleFieldChanged);
            this.updApertureAngle.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.OnTextBoxDimensionKeyPress);
            // 
            // lblAperture
            // 
            this.lblAperture.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblAperture.AutoSize = true;
            this.lblAperture.Location = new System.Drawing.Point(35, 74);
            this.lblAperture.Name = "lblAperture";
            this.lblAperture.Size = new System.Drawing.Size(60, 13);
            this.lblAperture.TabIndex = 3;
            this.lblAperture.Text = "Aperture (°)";
            this.lblAperture.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // updInnerDiameter
            // 
            this.updInnerDiameter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.updInnerDiameter.Location = new System.Drawing.Point(126, 19);
            this.updInnerDiameter.Name = "updInnerDiameter";
            this.updInnerDiameter.Size = new System.Drawing.Size(65, 20);
            this.updInnerDiameter.TabIndex = 0;
            this.updInnerDiameter.ValueChanged += new System.EventHandler(this.OnInnerDiameterFieldChanged);
            this.updInnerDiameter.Enter += new System.EventHandler(this.OnEnterInnerDiameterField);
            this.updInnerDiameter.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.OnTextBoxDimensionKeyPress);
            // 
            // lblCupType
            // 
            this.lblCupType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCupType.AutoSize = true;
            this.lblCupType.Location = new System.Drawing.Point(55, 48);
            this.lblCupType.Name = "lblCupType";
            this.lblCupType.Size = new System.Drawing.Size(53, 13);
            this.lblCupType.TabIndex = 1;
            this.lblCupType.Text = "Cup Type";
            this.lblCupType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDiameter
            // 
            this.lblDiameter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblDiameter.AutoSize = true;
            this.lblDiameter.Location = new System.Drawing.Point(22, 22);
            this.lblDiameter.Name = "lblDiameter";
            this.lblDiameter.Size = new System.Drawing.Size(94, 13);
            this.lblDiameter.TabIndex = 0;
            this.lblDiameter.Text = "Lateral Diam. (mm)";
            this.lblDiameter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlOrientation
            // 
            this.pnlOrientation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlOrientation.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlOrientation.Controls.Add(this.updInclination);
            this.pnlOrientation.Controls.Add(this.lblInclination);
            this.pnlOrientation.Controls.Add(this.updAnteversion);
            this.pnlOrientation.Controls.Add(this.lblAnteversion);
            this.pnlOrientation.Location = new System.Drawing.Point(0, 305);
            this.pnlOrientation.Name = "pnlOrientation";
            this.pnlOrientation.Size = new System.Drawing.Size(225, 74);
            this.pnlOrientation.TabIndex = 14;
            this.pnlOrientation.TabStop = false;
            this.pnlOrientation.Text = "Orientation";
            // 
            // updInclination
            // 
            this.updInclination.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.updInclination.Location = new System.Drawing.Point(138, 45);
            this.updInclination.Maximum = new decimal(new int[] {
            180,
            0,
            0,
            0});
            this.updInclination.Name = "updInclination";
            this.updInclination.Size = new System.Drawing.Size(53, 20);
            this.updInclination.TabIndex = 9;
            this.updInclination.ValueChanged += new System.EventHandler(this.OnInclinationFieldValueChanged);
            this.updInclination.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.NumericTextBoxKeyPress);
            // 
            // lblInclination
            // 
            this.lblInclination.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblInclination.AutoSize = true;
            this.lblInclination.Location = new System.Drawing.Point(47, 47);
            this.lblInclination.Name = "lblInclination";
            this.lblInclination.Size = new System.Drawing.Size(55, 13);
            this.lblInclination.TabIndex = 9;
            this.lblInclination.Text = "Inclination";
            this.lblInclination.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // updAnteversion
            // 
            this.updAnteversion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.updAnteversion.Location = new System.Drawing.Point(138, 19);
            this.updAnteversion.Maximum = new decimal(new int[] {
            90,
            0,
            0,
            0});
            this.updAnteversion.Minimum = new decimal(new int[] {
            90,
            0,
            0,
            -2147483648});
            this.updAnteversion.Name = "updAnteversion";
            this.updAnteversion.Size = new System.Drawing.Size(53, 20);
            this.updAnteversion.TabIndex = 8;
            this.updAnteversion.ValueChanged += new System.EventHandler(this.OnAnteversionFieldValueChanged);
            this.updAnteversion.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.NumericTextBoxKeyPress);
            // 
            // lblAnteversion
            // 
            this.lblAnteversion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblAnteversion.AutoSize = true;
            this.lblAnteversion.Location = new System.Drawing.Point(39, 21);
            this.lblAnteversion.Name = "lblAnteversion";
            this.lblAnteversion.Size = new System.Drawing.Size(63, 13);
            this.lblAnteversion.TabIndex = 0;
            this.lblAnteversion.Text = "Anteversion";
            this.lblAnteversion.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlPosition
            // 
            this.pnlPosition.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlPosition.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlPosition.Controls.Add(this.btnPos);
            this.pnlPosition.Controls.Add(this.btnAnt);
            this.pnlPosition.Controls.Add(this.btnLat);
            this.pnlPosition.Controls.Add(this.btnMed);
            this.pnlPosition.Controls.Add(this.btnInf);
            this.pnlPosition.Controls.Add(this.btnSup);
            this.pnlPosition.Controls.Add(this.lblAntPosUnit);
            this.pnlPosition.Controls.Add(this.lblMedLatUnit);
            this.pnlPosition.Controls.Add(this.lblSupInfUnit);
            this.pnlPosition.Controls.Add(this.lblReference);
            this.pnlPosition.Controls.Add(this.refPointList);
            this.pnlPosition.Controls.Add(this.txtSupInf);
            this.pnlPosition.Controls.Add(this.txtAntPos);
            this.pnlPosition.Controls.Add(this.txtMedLat);
            this.pnlPosition.Location = new System.Drawing.Point(0, 155);
            this.pnlPosition.Name = "pnlPosition";
            this.pnlPosition.Size = new System.Drawing.Size(225, 144);
            this.pnlPosition.TabIndex = 13;
            this.pnlPosition.TabStop = false;
            this.pnlPosition.Text = "Position";
            // 
            // btnPos
            // 
            this.btnPos.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPos.Location = new System.Drawing.Point(90, 80);
            this.btnPos.Name = "btnPos";
            this.btnPos.Size = new System.Drawing.Size(41, 23);
            this.btnPos.TabIndex = 22;
            this.btnPos.Text = "Pos";
            this.btnPos.UseVisualStyleBackColor = true;
            this.btnPos.Click += new System.EventHandler(this.OnClickPosteriorButton);
            // 
            // btnAnt
            // 
            this.btnAnt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAnt.Location = new System.Drawing.Point(43, 80);
            this.btnAnt.Name = "btnAnt";
            this.btnAnt.Size = new System.Drawing.Size(41, 23);
            this.btnAnt.TabIndex = 21;
            this.btnAnt.Text = "Ant";
            this.btnAnt.UseVisualStyleBackColor = true;
            this.btnAnt.Click += new System.EventHandler(this.OnClickAnteriorButton);
            // 
            // btnLat
            // 
            this.btnLat.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLat.Location = new System.Drawing.Point(90, 51);
            this.btnLat.Name = "btnLat";
            this.btnLat.Size = new System.Drawing.Size(41, 23);
            this.btnLat.TabIndex = 20;
            this.btnLat.Text = "Lat";
            this.btnLat.UseVisualStyleBackColor = true;
            this.btnLat.Click += new System.EventHandler(this.OnClickLateralButton);
            // 
            // btnMed
            // 
            this.btnMed.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnMed.Location = new System.Drawing.Point(43, 51);
            this.btnMed.Name = "btnMed";
            this.btnMed.Size = new System.Drawing.Size(41, 23);
            this.btnMed.TabIndex = 19;
            this.btnMed.Text = "Med";
            this.btnMed.UseVisualStyleBackColor = true;
            this.btnMed.Click += new System.EventHandler(this.OnClickMedialButton);
            // 
            // btnInf
            // 
            this.btnInf.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnInf.Location = new System.Drawing.Point(90, 22);
            this.btnInf.Name = "btnInf";
            this.btnInf.Size = new System.Drawing.Size(41, 23);
            this.btnInf.TabIndex = 18;
            this.btnInf.Text = "Inf";
            this.btnInf.UseVisualStyleBackColor = true;
            this.btnInf.Click += new System.EventHandler(this.OnClickInferiorButton);
            // 
            // btnSup
            // 
            this.btnSup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSup.Location = new System.Drawing.Point(43, 22);
            this.btnSup.Name = "btnSup";
            this.btnSup.Size = new System.Drawing.Size(41, 23);
            this.btnSup.TabIndex = 17;
            this.btnSup.Text = "Sup";
            this.btnSup.UseVisualStyleBackColor = true;
            this.btnSup.Click += new System.EventHandler(this.OnClickSuperiorButton);
            // 
            // lblAntPosUnit
            // 
            this.lblAntPosUnit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblAntPosUnit.AutoSize = true;
            this.lblAntPosUnit.Location = new System.Drawing.Point(196, 84);
            this.lblAntPosUnit.Name = "lblAntPosUnit";
            this.lblAntPosUnit.Size = new System.Drawing.Size(23, 13);
            this.lblAntPosUnit.TabIndex = 12;
            this.lblAntPosUnit.Text = "mm";
            // 
            // lblMedLatUnit
            // 
            this.lblMedLatUnit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblMedLatUnit.AutoSize = true;
            this.lblMedLatUnit.Location = new System.Drawing.Point(196, 55);
            this.lblMedLatUnit.Name = "lblMedLatUnit";
            this.lblMedLatUnit.Size = new System.Drawing.Size(23, 13);
            this.lblMedLatUnit.TabIndex = 11;
            this.lblMedLatUnit.Text = "mm";
            // 
            // lblSupInfUnit
            // 
            this.lblSupInfUnit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSupInfUnit.AutoSize = true;
            this.lblSupInfUnit.Location = new System.Drawing.Point(196, 26);
            this.lblSupInfUnit.Name = "lblSupInfUnit";
            this.lblSupInfUnit.Size = new System.Drawing.Size(23, 13);
            this.lblSupInfUnit.TabIndex = 10;
            this.lblSupInfUnit.Text = "mm";
            // 
            // lblReference
            // 
            this.lblReference.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblReference.AutoSize = true;
            this.lblReference.Location = new System.Drawing.Point(44, 113);
            this.lblReference.Name = "lblReference";
            this.lblReference.Size = new System.Drawing.Size(57, 13);
            this.lblReference.TabIndex = 9;
            this.lblReference.Text = "Reference";
            this.lblReference.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // refPointList
            // 
            this.refPointList.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.refPointList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.refPointList.FormattingEnabled = true;
            this.refPointList.ItemHeight = 13;
            this.refPointList.Location = new System.Drawing.Point(125, 110);
            this.refPointList.Name = "refPointList";
            this.refPointList.Size = new System.Drawing.Size(94, 21);
            this.refPointList.TabIndex = 8;
            this.refPointList.SelectedIndexChanged += new System.EventHandler(this.OnSelectedReferencePointChanged);
            // 
            // txtSupInf
            // 
            this.txtSupInf.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSupInf.Location = new System.Drawing.Point(137, 24);
            this.txtSupInf.Name = "txtSupInf";
            this.txtSupInf.Size = new System.Drawing.Size(53, 20);
            this.txtSupInf.TabIndex = 3;
            this.txtSupInf.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.NumericTextBoxKeyPress);
            this.txtSupInf.KeyUp += new System.Windows.Forms.KeyEventHandler(this.OnTextboxInferiorSuperiorKeyUp);
            this.txtSupInf.Leave += new System.EventHandler(this.OnTextboxInferiorSuperiorLeave);
            // 
            // txtAntPos
            // 
            this.txtAntPos.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAntPos.Location = new System.Drawing.Point(137, 82);
            this.txtAntPos.Name = "txtAntPos";
            this.txtAntPos.Size = new System.Drawing.Size(53, 20);
            this.txtAntPos.TabIndex = 3;
            this.txtAntPos.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.NumericTextBoxKeyPress);
            this.txtAntPos.KeyUp += new System.Windows.Forms.KeyEventHandler(this.OnTextboxAnteriorPosteriorKeyUp);
            this.txtAntPos.Leave += new System.EventHandler(this.OnTextboxAnteriorPosteriorLeave);
            // 
            // txtMedLat
            // 
            this.txtMedLat.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMedLat.Location = new System.Drawing.Point(137, 53);
            this.txtMedLat.Name = "txtMedLat";
            this.txtMedLat.Size = new System.Drawing.Size(53, 20);
            this.txtMedLat.TabIndex = 3;
            this.txtMedLat.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.NumericTextBoxKeyPress);
            this.txtMedLat.KeyUp += new System.Windows.Forms.KeyEventHandler(this.OnTextboxMedialLateralKeyUp);
            this.txtMedLat.Leave += new System.EventHandler(this.OnTextboxMedialLateralLeave);
            // 
            // pnlRbv
            // 
            this.pnlRbv.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlRbv.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlRbv.Controls.Add(this.chkRbvPreview);
            this.pnlRbv.Location = new System.Drawing.Point(0, 385);
            this.pnlRbv.Name = "pnlRbv";
            this.pnlRbv.Size = new System.Drawing.Size(225, 64);
            this.pnlRbv.TabIndex = 18;
            this.pnlRbv.TabStop = false;
            this.pnlRbv.Text = "RBV";
            // 
            // chkRbvPreview
            // 
            this.chkRbvPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkRbvPreview.Location = new System.Drawing.Point(19, 24);
            this.chkRbvPreview.Name = "chkRbvPreview";
            this.chkRbvPreview.Size = new System.Drawing.Size(192, 23);
            this.chkRbvPreview.TabIndex = 10;
            this.chkRbvPreview.Text = "Show preview entity";
            this.chkRbvPreview.UseVisualStyleBackColor = true;
            this.chkRbvPreview.CheckedChanged += new System.EventHandler(this.OnShowReamingCheckedChanged);
            // 
            // pnlVisualisation
            // 
            this.pnlVisualisation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlVisualisation.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlVisualisation.Controls.Add(this.btnCupView);
            this.pnlVisualisation.Controls.Add(this.btnInclinationView);
            this.pnlVisualisation.Controls.Add(this.btnAnteversionView);
            this.pnlVisualisation.Controls.Add(this.chkCupVector);
            this.pnlVisualisation.Controls.Add(this.chkInclination);
            this.pnlVisualisation.Controls.Add(this.chkAnteversion);
            this.pnlVisualisation.Location = new System.Drawing.Point(0, 455);
            this.pnlVisualisation.Name = "pnlVisualisation";
            this.pnlVisualisation.Size = new System.Drawing.Size(225, 100);
            this.pnlVisualisation.TabIndex = 19;
            this.pnlVisualisation.TabStop = false;
            this.pnlVisualisation.Text = "Visualisation";
            // 
            // btnCupView
            // 
            this.btnCupView.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCupView.Location = new System.Drawing.Point(136, 65);
            this.btnCupView.Name = "btnCupView";
            this.btnCupView.Size = new System.Drawing.Size(75, 23);
            this.btnCupView.TabIndex = 16;
            this.btnCupView.Text = "Cup View";
            this.btnCupView.UseVisualStyleBackColor = true;
            this.btnCupView.Click += new System.EventHandler(this.OnCupViewClick);
            // 
            // btnInclinationView
            // 
            this.btnInclinationView.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnInclinationView.Location = new System.Drawing.Point(136, 42);
            this.btnInclinationView.Name = "btnInclinationView";
            this.btnInclinationView.Size = new System.Drawing.Size(75, 23);
            this.btnInclinationView.TabIndex = 15;
            this.btnInclinationView.Text = "INCL View";
            this.btnInclinationView.UseVisualStyleBackColor = true;
            this.btnInclinationView.Click += new System.EventHandler(this.OnInclinationViewClick);
            // 
            // btnAnteversionView
            // 
            this.btnAnteversionView.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAnteversionView.Location = new System.Drawing.Point(136, 19);
            this.btnAnteversionView.Name = "btnAnteversionView";
            this.btnAnteversionView.Size = new System.Drawing.Size(75, 23);
            this.btnAnteversionView.TabIndex = 14;
            this.btnAnteversionView.Text = "AV View";
            this.btnAnteversionView.UseVisualStyleBackColor = true;
            this.btnAnteversionView.Click += new System.EventHandler(this.OnAnteversionViewClick);
            // 
            // chkCupVector
            // 
            this.chkCupVector.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkCupVector.AutoSize = true;
            this.chkCupVector.Location = new System.Drawing.Point(12, 69);
            this.chkCupVector.Name = "chkCupVector";
            this.chkCupVector.Size = new System.Drawing.Size(79, 17);
            this.chkCupVector.TabIndex = 13;
            this.chkCupVector.Text = "Cup Vector";
            this.chkCupVector.UseVisualStyleBackColor = true;
            this.chkCupVector.CheckedChanged += new System.EventHandler(this.OnOrientationConduitCheckChanged);
            // 
            // chkInclination
            // 
            this.chkInclination.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkInclination.AutoSize = true;
            this.chkInclination.Location = new System.Drawing.Point(13, 46);
            this.chkInclination.Name = "chkInclination";
            this.chkInclination.Size = new System.Drawing.Size(117, 17);
            this.chkInclination.TabIndex = 12;
            this.chkInclination.Text = "INCL Measurement";
            this.chkInclination.UseVisualStyleBackColor = true;
            this.chkInclination.CheckedChanged += new System.EventHandler(this.OnOrientationConduitCheckChanged);
            // 
            // chkAnteversion
            // 
            this.chkAnteversion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkAnteversion.AutoSize = true;
            this.chkAnteversion.Location = new System.Drawing.Point(13, 23);
            this.chkAnteversion.Name = "chkAnteversion";
            this.chkAnteversion.Size = new System.Drawing.Size(107, 17);
            this.chkAnteversion.TabIndex = 11;
            this.chkAnteversion.Text = "AV Measurement";
            this.chkAnteversion.UseVisualStyleBackColor = true;
            this.chkAnteversion.CheckedChanged += new System.EventHandler(this.OnOrientationConduitCheckChanged);
            // 
            // pnlFlanges
            // 
            this.pnlFlanges.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlFlanges.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlFlanges.Controls.Add(this.updFlangeThickness);
            this.pnlFlanges.Controls.Add(this.lblFlangeThickness);
            this.pnlFlanges.Location = new System.Drawing.Point(0, 561);
            this.pnlFlanges.Name = "pnlFlanges";
            this.pnlFlanges.Size = new System.Drawing.Size(225, 59);
            this.pnlFlanges.TabIndex = 20;
            this.pnlFlanges.TabStop = false;
            this.pnlFlanges.Text = "Flanges";
            // 
            // updFlangeThickness
            // 
            this.updFlangeThickness.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.updFlangeThickness.Location = new System.Drawing.Point(137, 29);
            this.updFlangeThickness.Maximum = new decimal(new int[] {
            4,
            0,
            0,
            0});
            this.updFlangeThickness.Minimum = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.updFlangeThickness.Name = "updFlangeThickness";
            this.updFlangeThickness.Size = new System.Drawing.Size(65, 20);
            this.updFlangeThickness.TabIndex = 5;
            this.updFlangeThickness.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.updFlangeThickness.ValueChanged += new System.EventHandler(this.UpdateFlangeThickness_ValueChanged);
            this.updFlangeThickness.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.OnTextBoxDimensionKeyPress);
            // 
            // lblFlangeThickness
            // 
            this.lblFlangeThickness.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblFlangeThickness.AutoSize = true;
            this.lblFlangeThickness.Location = new System.Drawing.Point(22, 31);
            this.lblFlangeThickness.Name = "lblFlangeThickness";
            this.lblFlangeThickness.Size = new System.Drawing.Size(81, 13);
            this.lblFlangeThickness.TabIndex = 4;
            this.lblFlangeThickness.Text = "Thickness (mm)";
            this.lblFlangeThickness.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.Location = new System.Drawing.Point(11, 12);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(79, 20);
            this.lblTitle.TabIndex = 21;
            this.lblTitle.Text = "IDS - Cup";
            // 
            // CupPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.pnlFlanges);
            this.Controls.Add(this.pnlVisualisation);
            this.Controls.Add(this.pnlRbv);
            this.Controls.Add(this.pnlDimensions);
            this.Controls.Add(this.pnlOrientation);
            this.Controls.Add(this.pnlPosition);
            this.MinimumSize = new System.Drawing.Size(225, 520);
            this.Name = "CupPanel";
            this.Size = new System.Drawing.Size(225, 623);
            this.EnabledChanged += new System.EventHandler(this.OnCupPanelEnabledChanged);
            this.VisibleChanged += new System.EventHandler(this.OnVisibilityChanged);
            this.Resize += new System.EventHandler(this.CupPanel_Resize);
            this.pnlDimensions.ResumeLayout(false);
            this.pnlDimensions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.updApertureAngle)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.updInnerDiameter)).EndInit();
            this.pnlOrientation.ResumeLayout(false);
            this.pnlOrientation.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.updInclination)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.updAnteversion)).EndInit();
            this.pnlPosition.ResumeLayout(false);
            this.pnlPosition.PerformLayout();
            this.pnlRbv.ResumeLayout(false);
            this.pnlVisualisation.ResumeLayout(false);
            this.pnlVisualisation.PerformLayout();
            this.pnlFlanges.ResumeLayout(false);
            this.pnlFlanges.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.updFlangeThickness)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox pnlDimensions;
        private System.Windows.Forms.GroupBox pnlOrientation;
        private System.Windows.Forms.Label lblInclination;
        private System.Windows.Forms.Label lblAnteversion;
        private System.Windows.Forms.GroupBox pnlPosition;
        public System.Windows.Forms.NumericUpDown updInclination;
        public System.Windows.Forms.NumericUpDown updAnteversion;
        private System.Windows.Forms.Label lblCupType;
        private System.Windows.Forms.Label lblDiameter;
        private System.Windows.Forms.Label lblAperture;
        public System.Windows.Forms.NumericUpDown updInnerDiameter;
        public System.Windows.Forms.NumericUpDown updApertureAngle;
        private System.Windows.Forms.Label lblReference;
        public System.Windows.Forms.ComboBox refPointList;
        private System.Windows.Forms.GroupBox pnlRbv;
        private System.Windows.Forms.Label lblSupInfUnit;
        private System.Windows.Forms.Label lblAntPosUnit;
        private System.Windows.Forms.Label lblMedLatUnit;
        private System.Windows.Forms.GroupBox pnlVisualisation;
        public System.Windows.Forms.CheckBox chkInclination;
        public System.Windows.Forms.CheckBox chkAnteversion;
        public System.Windows.Forms.Button btnCupView;
        public System.Windows.Forms.Button btnInclinationView;
        public System.Windows.Forms.Button btnAnteversionView;
        public System.Windows.Forms.CheckBox chkCupVector;
        private System.Windows.Forms.ComboBox cmbCupType;
        public System.Windows.Forms.CheckBox chkRbvPreview;
        private System.Windows.Forms.Button btnPos;
        private System.Windows.Forms.Button btnAnt;
        private System.Windows.Forms.Button btnLat;
        private System.Windows.Forms.Button btnMed;
        private System.Windows.Forms.Button btnInf;
        private System.Windows.Forms.Button btnSup;
        public System.Windows.Forms.TextBox txtSupInf;
        public System.Windows.Forms.TextBox txtAntPos;
        public System.Windows.Forms.TextBox txtMedLat;
        private System.Windows.Forms.GroupBox pnlFlanges;
        public System.Windows.Forms.NumericUpDown updFlangeThickness;
        private System.Windows.Forms.Label lblFlangeThickness;
        private System.Windows.Forms.Label lblTitle;
    }
}
