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

            if (comboBox2.SelectedItem == null || comboBox3.SelectedItem == null)
            {
                MessageBox.Show("Select both margins.");
                return;
            }

            Structure selectedStructure = currentStructureSet.Structures
                .FirstOrDefault(s => s.Id == structureId);

            if (selectedStructure == null)
            {
                MessageBox.Show("Structure not found.");
                return;
            }

            int innerMargin = (int)comboBox2.SelectedItem;
            int outerMargin = (int)comboBox3.SelectedItem;

            if (outerMargin <= innerMargin)
            {
                MessageBox.Show("Outer margin must be greater than inner margin.");
                return;
            }

            //------------------------------------------
            // Create INNER structure
            //------------------------------------------

            string innerId = $"{selectedStructure.Id}_inner";

            if (!currentStructureSet.CanAddStructure("CONTROL", innerId))
            {
                MessageBox.Show($"{innerId} already exists.");
                return;
            }

            Structure innerStructure =
                currentStructureSet.AddStructure("CONTROL", innerId);

            if (selectedStructure.IsHighResolution)
                innerStructure.ConvertToHighResolution();

            SegmentVolume innerVolume = selectedStructure.Margin(innerMargin);

            if (innerVolume == null)
            {
                MessageBox.Show("Unable to create inner margin.");
                return;
            }

            innerStructure.SegmentVolume = innerVolume;

            //------------------------------------------
            // Create OUTER structure
            //------------------------------------------

            string outerId = $"{selectedStructure.Id}_outer";

            if (!currentStructureSet.CanAddStructure("CONTROL", outerId))
            {
                MessageBox.Show($"{outerId} already exists.");
                return;
            }

            Structure outerStructure =
                currentStructureSet.AddStructure("CONTROL", outerId);

            if (selectedStructure.IsHighResolution)
                outerStructure.ConvertToHighResolution();

            SegmentVolume outerVolume = selectedStructure.Margin(outerMargin);

            if (outerVolume == null)
            {
                MessageBox.Show("Unable to create outer margin.");
                return;
            }

            outerStructure.SegmentVolume = outerVolume;

            //------------------------------------------
            // Create Ring
            //------------------------------------------

            string ringId = $"{selectedStructure.Id}_Ring";

            if (!currentStructureSet.CanAddStructure("CONTROL", ringId))
            {
                MessageBox.Show($"{ringId} already exists.");
                return;
            }

            Structure ringStructure =
                currentStructureSet.AddStructure("CONTROL", ringId);

            if (selectedStructure.IsHighResolution)
                ringStructure.ConvertToHighResolution();

            SegmentVolume ringVolume =
                outerStructure.SegmentVolume.Sub(innerStructure.SegmentVolume);

            if (ringVolume == null)
            {
                MessageBox.Show("Boolean subtraction failed.");
                return;
            }

            ringStructure.SegmentVolume = ringVolume;

            //------------------------------------------
            // Remove temporary structures
            //------------------------------------------

            currentStructureSet.RemoveStructure(innerStructure);
            currentStructureSet.RemoveStructure(outerStructure);

            app.SaveModifications();

            MessageBox.Show($"{ringStructure.Id} created successfully.");

            app.ClosePatient();

            // Clear patient search box and reset UI for new search
            textBox1.Text = string.Empty;
            clearComboBoxes();
            comboBox4.ItemsSource = null;
            comboBox4.SelectedItem = null;
            currentPatient = null;
            currentStructureSet = null;
            currentCourse = null;
        }
        //teste para subir o projeto no GitHub

        //teste 2 para GitHub

        
    }

}

