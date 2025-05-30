using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

[assembly: ESAPIScript(IsWriteable = true)]

namespace Aneis_e_estruturas_de_OTM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Create conexion with Eclipse
        VMS.TPS.Common.Model.API.Application app;

        Patient currentPatient;
        StructureSet currentStructureSet;
        Course currentCourse;
        public MainWindow()
        {
            InitializeComponent();

            app = VMS.TPS.Common.Model.API.Application.CreateApplication();

            //ComboBox Lists
            comboBox2.ItemsSource = new List<int> { 0, 10, 20, 30 };
            comboBox3.ItemsSource = new List<int> { 10, 20, 30 };

        }

        //Clear all combobox befor select patient

        private void clearComboBoxes()
        {
            comboBox1.ItemsSource = null;
            comboBox2.ItemsSource = null;
            comboBox3.ItemsSource = null;
           // comboBox4.ItemsSource = null;

            comboBox1.SelectedItem = null;
            comboBox2.SelectedItem = null;
            comboBox3.SelectedItem = null;
            // comboBox4.SelectedItem = null;

            currentStructureSet = null;             
        }

        //Button to open the patient
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            String patientId = textBox1.Text;

            if (string.IsNullOrWhiteSpace(patientId))
            {
                textBox1.Text = "Insert a valid patient ID";
                return;
            }

            try
            {
                if(currentPatient != null)
                {
                    app.ClosePatient();
                    currentPatient = null;
                    
                }
                               

                //clear all combobox
                clearComboBoxes();

                //Declare patient opened by ID
                currentPatient = app.OpenPatientById(patientId);
                currentPatient.BeginModifications();

                if (currentPatient != null)
                {
                    textBox1.Text = $"Patient {currentPatient.Name} was successful open.";
                }
                else
                {
                    textBox1.Text = "Patient did not finded.";
                }

                //Declare structureSet of combobox


                var currentStructureSet = currentPatient.StructureSets.Select(sss => sss.Id).ToList();
                comboBox4.ItemsSource = currentStructureSet;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void comboBox4_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (currentPatient == null)
            {
                MessageBox.Show("Patient not loaded.");
                return;
            }
            if (comboBox4.SelectedItem is string selectedStructureSetId)
            {
                if (currentStructureSet != null && currentStructureSet.Id == selectedStructureSetId)
                {
                    return;
                }

                clearComboBoxes();

                currentStructureSet = currentPatient.StructureSets.FirstOrDefault(sss => sss.Id == selectedStructureSetId);

                if (currentStructureSet != null)
                {
                    var currenStructures = currentStructureSet.Structures.Where(sss => !sss.IsEmpty && sss.HasSegment).Select(sss => sss.Id).ToList();

                    comboBox1.ItemsSource = currenStructures;

                    comboBox2.ItemsSource = new List<int> { 0,10,20,30 };
                    comboBox3.ItemsSource = new List<int> { 10,20,30 };
                }
                else
                {
                    MessageBox.Show("Structureset not found.");
                }
            }
        }

        //Select margin from Target/OAR
        private void comboBox2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox2.SelectedItem is int selectedMargin)
            {
                comboBox2.SelectedIndex = selectedMargin;
            }
        }


        //Select the size of Ring
        private void comboBox3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (comboBox3.SelectedItem is int selectedRing)
            {
                comboBox3.SelectedIndex = selectedRing;
            }
        }

        //Create the Ring
        private void Button_Click2(object sender, RoutedEventArgs e)
        {
            string structureId = comboBox1.SelectedItem as string;
            if (string.IsNullOrEmpty(structureId))
            {
                MessageBox.Show("Select a structure.");
                return;
            }
            if (comboBox2.SelectedItem == null)
            {
                MessageBox.Show("Select a margin.");
                return;
            }

            //Declare the selected structure from combobox
            Structure selectedStructure = currentStructureSet.Structures.FirstOrDefault(sss => sss.Id == structureId);

            if (selectedStructure == null)
            {
                MessageBox.Show("Structure was not found.");
                return;
            }

            int margin1 = (int)comboBox2.SelectedItem;

            //message of structure expanded
            string structureExpanded1 = $"{selectedStructure.Id} expanded by {margin1}cm";

            if (!currentStructureSet.CanAddStructure("CONTROL", structureExpanded1))
            {
                MessageBox.Show("The name of structure already exists or is invalid.");
                return;
            }
            else
            {
                Structure structureMargin = currentStructureSet.AddStructure("CONTROL", structureExpanded1);
                structureMargin.SegmentVolume = selectedStructure.Margin(margin1);

                structureMargin.Id = $"{selectedStructure.Id}_inner";

                MessageBox.Show($"{structureExpanded1} was created.");

            }

            //Margin for second structure expanded
            int margin2 = (int)comboBox3.SelectedItem;

            string structureExpanded2 = $"{selectedStructure.Id} expanded by {margin2}cm";

            if (!currentStructureSet.CanAddStructure("CONTROL", structureExpanded2))
            {
                MessageBox.Show("The name of structure already exists or is invalid.");
                return;
            }
            else
            {
                Structure structureMargin2 = currentStructureSet.AddStructure("CONTROL", structureExpanded2);
                structureMargin2.SegmentVolume = selectedStructure.Margin(margin2);

                structureMargin2.Id = $"{selectedStructure.Id}_outer";
                MessageBox.Show($"{structureExpanded2} was created.");

            }

            
            Structure structureRing = currentStructureSet.AddStructure("CONTROL", $"{selectedStructure}_Ring");

            Structure innerStructure = currentStructureSet.Structures.FirstOrDefault(sss => sss.Id.Contains("inner"));

            Structure outterStructure = currentStructureSet.Structures.FirstOrDefault(sss => sss.Id.Contains("outer"));
            structureRing.SegmentVolume = outterStructure.Sub(innerStructure.SegmentVolume);

            currentStructureSet.RemoveStructure(innerStructure);
            currentStructureSet.RemoveStructure(outterStructure);

            MessageBox.Show($"The {structureRing} was successfull created.");

            app.SaveModifications();
            app.ClosePatient();

        }


    }

}

