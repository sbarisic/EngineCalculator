using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using unvell.ReoGrid;
using unvell.ReoGrid.Graphics;
using unvell.ReoGrid.Drawing.Shapes;
using System.IO;
using System.Security.Cryptography;
using System.Data.SqlTypes;

namespace EngineCalculator {
    public delegate void OnCellAction(int X, int Y, ref Cell C);

    public partial class TableConvert : Form {
        EditableData CurrentEdited;
        GridSelection Selection;

        public TableConvert() {
            InitializeComponent();
        }

        private void TableConvert_Load(object sender, EventArgs e) {
            Grid.DisableSettings(WorkbookSettings.View_ShowSheetTabControl);
        }

        bool CalculateSelection() {
            Worksheet Sheet = Grid.CurrentWorksheet;
            RangePosition CurRange = Sheet.SelectionRange;

            if (Selection == null)
                Selection = new GridSelection();

            Selection.Sheet = Sheet;
            Selection.A = Sheet.Cells[CurRange.StartPos];
            Selection.D = Sheet.Cells[CurRange.EndPos];
            Selection.B = Sheet.Cells[Selection.A.Row, Selection.D.Column];
            Selection.C = Sheet.Cells[Selection.D.Row, Selection.A.Column];

            if (Selection == null)
                return false;

            return true;
        }

        void ForEachCell(Worksheet Sheet, OnCellAction OnCell) {
            if (Sheet == null)
                return;

            for (int X = 0; X < Sheet.ColumnCount; X++)
                for (int Y = 0; Y < Sheet.RowCount; Y++) {
                    Cell CurCell = Sheet.Cells[Y, X];
                    OnCell(X, Y, ref CurCell);
                }
        }

        void ForEachSelectedCell(Worksheet Sheet, RangePosition Rng, OnCellAction OnCell) {
            ReferenceRange RefRng = Sheet.Ranges[Rng];

            foreach (Cell C in RefRng.Cells) {
                Cell CurCell = C;
                OnCell(CurCell.Column, CurCell.Row, ref CurCell);
            }
        }

        void Interpolate(GridSelection Sel) {
            Cell A = Sel.A, B = Sel.B, C = Sel.C, D = Sel.D;

            for (int Y = Sel.Y1; Y <= Sel.Y2; Y++)
                for (int X = Sel.X1; X <= Sel.X2; X++) {
                    double Data;

                    if (Sel.Width == 1)
                        Data = Utils.Lerp((double)A.Data, (double)D.Data, Sel.Y1, Sel.Y2, Y);
                    else if (Sel.Height == 1)
                        Data = Utils.Lerp((double)A.Data, (double)B.Data, Sel.X1, Sel.X2, X);
                    else
                        Data = Utils.Bilinear((double)A.Data, (double)B.Data, (double)C.Data, (double)D.Data, Sel.X1, Sel.X2, Sel.Y1, Sel.Y2, X, Y);

                    Sel.Sheet.Cells[Y, X].Data = Math.Round(Data, 2);
                }

            ColorSheet(Sel.Sheet);
        }

        void ColorSheet(Worksheet Sheet) {
            ForEachCell(Sheet, (int X, int Y, ref Cell C) => {
                CurrentEdited.ColorCell(X, Y, C.Data, ref C);
            });

            Grid.Refresh();
        }

        void StylizeSheet(Worksheet Sheet) {
            RangePosition Rng = new RangePosition(0, 0, Sheet.RowCount, Sheet.ColumnCount);

            WorksheetRangeStyle Style = Sheet.GetRangeStyles(Rng);
            Style.Flag = PlainStyleFlag.FontSize | PlainStyleFlag.FontName;
            Style.FontName = "Consolas";
            Style.FontSize = 9;

            Sheet.SetRangeStyles(Rng, Style);
        }

        public void Edit(EditableData Data) {
            CurrentEdited = Data;

            if (Data == null)
                return;

            if (!Data.DataEnabled)
                return;

            Grid.CurrentData = Data;

            switch (Data.EditMode) {
                case EditMode.Grid: {
                    Grid.Worksheets.Clear();

                    if (Data.Worksheet == null) {
                        Worksheet WSheet = Grid.Worksheets.Create(string.Format("{0} / {1}", Data.XAxisName, Data.YAxisName));
                        Data.Worksheet = WSheet;



                        WSheet.DisableSettings(WorksheetSettings.Edit_AllowAdjustColumnWidth | WorksheetSettings.Edit_AllowAdjustRowHeight);
                        WSheet.DisableSettings(WorksheetSettings.Edit_DragSelectionToMoveCells);

                        WSheet.RowCount = 1;
                        WSheet.ColumnCount = 1;

                        Data.PopulateSheet(WSheet);
                        // Interpolate(new GridSelection(WSheet));
                    }

                    Grid.CurrentWorksheet = Data.Worksheet;
                    ColorSheet(Data.Worksheet);
                    StylizeSheet(Data.Worksheet);
                    break;
                }

                default:
                    throw new Exception("Invalid edit mode " + Data.EditMode);
            }
        }

        EditableData CreateNewTable(string XName, double[] XData, string YName, double[] YData, double[] Data = null) {
            LookupTableAxis AxisX = new LookupTableAxis(XName, XData);
            LookupTableAxis AxisY = new LookupTableAxis(YName, YData);
            LookupTable2D NewTable = new LookupTable2D(AxisX, AxisY, Data);

            return new EditableData(EditMode.Grid, NewTable, "Unit");
        }

        void CalculateSpark() {
            LookupTable2D OrigTable;
            EditableData OrigData = Utils.ParseTableFromText(File.ReadAllText("data/HighOctane.txt"), out OrigTable);

            // Spark
            double[] New_X = new double[] { 30, 40, 50, 60, 70, 80, 90, 100, 125, 150, 175, 200, 225, 250, 275, 300 };
            double[] New_Y = new double[] { 900, 1200, 1400, 1600, 1800, 2000, 2200, 2600, 3000, 3400, 3800, 4200, 4600, 5000, 5400, 5800, 6200, 6600, 7000, 7400 }.Reverse().ToArray();

            double[] Data = new double[New_X.Length * New_Y.Length];
            EditableData EData = CreateNewTable("MAP", New_X, "RPM", New_Y, Data);
            LookupTable2D NewTable = EData.Table;

            float AirTemp = int.Parse(tbAirTemp.Text) + 273.15f;

            for (int Y = 0; Y < New_Y.Length; Y++) {
                for (int X = 0; X < New_X.Length; X++) {

                    float RPM = (float)New_Y[Y];
                    float MAP = (float)New_X[X];

                    //CylAirmass.CalcManifoldAbsolutePressure

                    float AirMass = CylAirmass.CalcAirmass(RPM, MAP * 1000, AirTemp);
                    double Val = Math.Round(Utils.Clamp(OrigTable.IndexData(RPM, AirMass), -60, 60), 0);

                    NewTable.SetData(X, Y, Val);
                }
            }

            /*foreach (var CylAirm in CylAirmasses) {
                     CylAirmass.CalcManifoldAbsolutePressure(1000, (float)OrigTable.Axis_Y.Data.Last(), 0);
                 }*/



            Edit(EData);
        }


        void CalculateInjectionAngle() {
            LookupTable2D OrigTable;
            EditableData OrigData = Utils.ParseTableFromText(File.ReadAllText("data/InjBoundary_NormalRPM.txt"), out OrigTable);

            LookupTable2D OrigTable_Base;
            EditableData OrigData_Base = Utils.ParseTableFromText(File.ReadAllText("data/InjBoundary.txt"), out OrigTable_Base);

            double[] New_X = new double[] { 20, 80, 90, 100, 110, 120, 150, 200 };
            double[] New_Y = new double[] { 500, 1500, 2000, 2500, 3000, 4000, 5000, 7000 }.Reverse().ToArray();

            double[] Data = new double[New_X.Length * New_Y.Length];
            EditableData EData = CreateNewTable("MAP", New_X, "RPM", New_Y, Data);
            LookupTable2D NewTable = EData.Table;

            float AirTemp = int.Parse(tbAirTemp.Text) + 273.15f;
            int Min = 0;
            int Max = 1440;
            int Decimals = 0;


            for (int Y = 0; Y < New_Y.Length; Y++) {
                for (int X = 0; X < New_X.Length; X++) {

                    float RPM = (float)New_Y[Y];
                    float MAP = (float)New_X[X];

                    //CylAirmass.CalcManifoldAbsolutePressure

                    float AirMass = CylAirmass.CalcAirmass(RPM, MAP * 1000, AirTemp);
                    double Val = OrigTable.IndexData(RPM, AirMass);

                    double Base = OrigTable_Base.IndexData(RPM, 0);
                    Val = Base;//- Val;

                    NewTable.SetData(X, Y, Math.Round(Utils.Clamp(Val, Min, Max), Decimals));
                }
            }

            /*foreach (var CylAirm in CylAirmasses) {
                     CylAirmass.CalcManifoldAbsolutePressure(1000, (float)OrigTable.Axis_Y.Data.Last(), 0);
                 }*/



            Edit(EData);
        }

        private void btnRecalculate_Click(object sender, EventArgs e) {
            //CalculateInjectionAngle();

            CalculateSpark();
        }

        string SerializeNum(string Storage, double Num) {
            switch (Storage) {
                case "sbyte": {
                    string Prefix = "";

                    if (Num < 0) {
                        Prefix = "-";
                        Num = -Num;
                    }

                    return Prefix + string.Format("{0:X}", (int)Num);
                }

                case "word":
                    return string.Format("{0:X}", (int)Num);

                default:
                    throw new NotImplementedException();
            }
        }

        void Save(string TableName, string Storage, double RawMult, LookupTable2D Tbl) {
            int Width = Tbl.Axis_X.AxisLength;
            int Height = Tbl.Axis_Y.AxisLength;
            //string TableName = "ignTable";
            //string Storage = "sbyte";

            string HexData = "";

            for (int Y = Height - 1; Y >= 0; Y--) {
                for (int X = 0; X < Width; X++) {
                    HexData += SerializeNum(Storage, Tbl.GetDataRaw(X, Y) * RawMult) + " ";
                }
            }

            using (MemoryStream MS = new MemoryStream()) {
                using (StreamWriter SW = new StreamWriter(MS)) {
                    SW.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    SW.WriteLine("<project version=\"1.0\">");
                    SW.WriteLine("    <tables>");
                    SW.WriteLine("        <symbol name=\"{0}\" storage=\"{1}\" width=\"{2}\" height=\"{3}\" data=\"{4}\"/>", TableName, Storage, Width, Height, HexData);
                    SW.WriteLine("    </tables>");
                    SW.WriteLine("</project>");
                    SW.Flush();
                }

                string OutFile = "data/out.emubt";

                if (File.Exists(OutFile))
                    File.Delete(OutFile);

                File.WriteAllBytes(OutFile, MS.ToArray());
            }
        }

        private void btnSave_Click(object sender, EventArgs e) {


            if (CurrentEdited == null)
                return;

            LookupTable2D Tbl = CurrentEdited.Table;
            Save("ignTable", "sbyte", 2, Tbl);

            //Save("injectionAngle", "word", 1, Tbl);



        }
    }
}
