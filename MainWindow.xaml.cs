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
                if (currentPatient != null)
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

                    comboBox2.ItemsSource = new List<int> { 0, 10, 20, 30 };
                    comboBox3.ItemsSource = new List<int> { 10, 20, 30 };
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

            //==========================================================
            // ETAPA 1 - CRIAR O RING
            //==========================================================

            string innerId = $"{selectedStructure.Id}_inner";
            string outerId = $"{selectedStructure.Id}_outer";
            string ringId = $"{selectedStructure.Id}_Ring";

            var innerStruct = currentStructureSet.Structures.FirstOrDefault(s => s.Id == innerId);
            if (innerStruct != null)
                currentStructureSet.RemoveStructure(innerStruct);

            var outerStruct = currentStructureSet.Structures.FirstOrDefault(s => s.Id == outerId);
            if (outerStruct != null)
                currentStructureSet.RemoveStructure(outerStruct);

            var ringStruct = currentStructureSet.Structures.FirstOrDefault(s => s.Id == ringId);
            if (ringStruct != null)
                currentStructureSet.RemoveStructure(ringStruct);

            if (!currentStructureSet.CanAddStructure("CONTROL", innerId))
            {
                MessageBox.Show($"{innerId} already exists and cannot be removed.");
                return;
            }

            Structure innerStructure = currentStructureSet.AddStructure("CONTROL", innerId);
            if (selectedStructure.IsHighResolution)
                innerStructure.ConvertToHighResolution();

            SegmentVolume innerVolume = selectedStructure.Margin(innerMargin);
            if (innerVolume == null)
            {
                MessageBox.Show("Unable to create inner margin.");
                return;
            }
            innerStructure.SegmentVolume = innerVolume;

            if (!currentStructureSet.CanAddStructure("CONTROL", outerId))
            {
                MessageBox.Show($"{outerId} already exists and cannot be removed.");
                return;
            }

            Structure outerStructure = currentStructureSet.AddStructure("CONTROL", outerId);
            if (selectedStructure.IsHighResolution)
                outerStructure.ConvertToHighResolution();

            SegmentVolume outerVolume = selectedStructure.Margin(outerMargin);
            if (outerVolume == null)
            {
                MessageBox.Show("Unable to create outer margin.");
                return;
            }
            outerStructure.SegmentVolume = outerVolume;

            if (!currentStructureSet.CanAddStructure("CONTROL", ringId))
            {
                MessageBox.Show($"{ringId} already exists and cannot be removed.");
                return;
            }

            Structure ringStructure = currentStructureSet.AddStructure("CONTROL", ringId);
            if (selectedStructure.IsHighResolution)
                ringStructure.ConvertToHighResolution();

            SegmentVolume ringVolume = outerStructure.SegmentVolume.Sub(innerStructure.SegmentVolume);
            if (ringVolume == null)
            {
                MessageBox.Show("Boolean subtraction failed.");
                return;
            }
            ringStructure.SegmentVolume = ringVolume;

            currentStructureSet.RemoveStructure(innerStructure);
            currentStructureSet.RemoveStructure(outerStructure);

            app.SaveModifications();   // salva o Ring antes de seguir pro crop

            //==========================================================
            // ETAPA 2 - CROPAR OS OARs (versão corrigida)
            //==========================================================
            //
            // A versão original (e minhas duas tentativas anteriores)
            // procuravam por QUALQUER estrutura com "PTV" no nome em todo
            // o structure set. Isso é o bug de verdade: se o plano tem
            // mais de uma estrutura com "PTV" no Id (boost, linfonodos,
            // outro PTV de outro curso, etc.), o crop podia pegar um PTV
            // que não tem nada a ver com o anel que acabou de ser criado
            // -- explicando tanto o corte inconsistente entre Reto/Bexiga
            // quanto o Reto "partido" e a Bexiga sumindo nas tentativas
            // anteriores (a união de PTVs errados gera uma geometria sem
            // sentido pra subtrair).
            //
            // A correção certa: usar o MESMO PTV que o usuário já
            // escolheu no combo pra criar o anel (selectedStructure) --
            // sem procurar por nome, sem ambiguidade.
            //
            // Também mantive o try/catch por OAR: antes, uma exceção em
            // qualquer OAR abortava o laço inteiro e os OARs seguintes
            // nunca eram processados, sem nenhuma mensagem de erro (este
            // projeto não tem handler de exceção não tratada no
            // App.xaml.cs).

            // Filtra só por NOME (Reto/Bladder, em português e inglês) em
            // vez de por DicomType == "ORGAN" -- foi isso que fazia a
            // Bexiga nunca ser cropada nesse structure set: ela está
            // tipada como "Avoidance" no Eclipse, não "Organ", então o
            // filtro antigo simplesmente pulava ela sem avisar nada.
            // Como agora o objetivo é só Reto e Bexiga (não qualquer
            // OAR do plano), o filtro por nome também é mais preciso.
            var oars = currentStructureSet.Structures
                .Where(s => !s.Id.ToUpper().Contains("OTM")
                    && !s.IsEmpty
                    && s.HasSegment
                    && (s.Id.IndexOf("Rectum", StringComparison.OrdinalIgnoreCase) >= 0
                        || s.Id.IndexOf("Reto", StringComparison.OrdinalIgnoreCase) >= 0
                        || s.Id.IndexOf("Bladder", StringComparison.OrdinalIgnoreCase) >= 0
                        || s.Id.IndexOf("Bexiga", StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();

            if (!oars.Any())
            {
                MessageBox.Show($"{ringStructure.Id} created successfully. No Rectum/Bladder structure found for cropping.");
                return;
            }

            int cropMargin = 5; // mm

            SegmentVolume ptvMargin = selectedStructure.Margin(cropMargin);
            if (ptvMargin == null)
            {
                MessageBox.Show($"{ringStructure.Id} created successfully. Could not compute margin from {selectedStructure.Id}.");
                return;
            }

            bool anyCropped = false;
            var falhas = new List<string>();

            foreach (var oar in oars)
            {
                try
                {
                    string otmId = $"{oar.Id}_OTM";

                    var existingOtm = currentStructureSet.Structures.FirstOrDefault(s => s.Id == otmId);
                    if (existingOtm != null)
                        currentStructureSet.RemoveStructure(existingOtm);

                    if (!currentStructureSet.CanAddStructure(oar.DicomType, otmId))
                    {
                        falhas.Add($"{oar.Id}: não foi possível criar {otmId}.");
                        continue;
                    }

                    var croppedVolume = oar.SegmentVolume.Sub(ptvMargin);
                    if (croppedVolume == null)
                    {
                        falhas.Add($"{oar.Id}: subtração retornou nulo.");
                        continue;
                    }

                    var otmStruct = currentStructureSet.AddStructure(oar.DicomType, otmId);
                    if (oar.IsHighResolution)
                        otmStruct.ConvertToHighResolution();

                    otmStruct.SegmentVolume = croppedVolume;
                    anyCropped = true;
                }
                catch (Exception ex)
                {
                    falhas.Add($"{oar.Id}: {ex.Message}");
                }
            }

            app.SaveModifications();   // salva tudo de uma vez só, no final

            //==========================================================
            // DIAGNÓSTICO PÓS-SAVE
            //==========================================================
            // Relemos Volume/IsEmpty DEPOIS do SaveModifications e
            // buscando a estrutura de novo pelo Id (em vez de reusar a
            // referência antiga) — se antes esses valores estavam sendo
            // lidos antes de o ESAPI "commitar" a geometria, isso corrige
            // o diagnóstico. Isso também é mais confiável pra decidir no
            // futuro se um _OTM deve ou não ser removido automaticamente.
            var linhasVolume = new List<string>();
            foreach (var oar in oars)
            {
                string otmId = $"{oar.Id}_OTM";
                var otm = currentStructureSet.Structures.FirstOrDefault(s => s.Id == otmId);
                if (otm == null)
                    linhasVolume.Add($"{oar.Id}: {otmId} não existe.");
                else
                    linhasVolume.Add($"{oar.Id}: original={oar.Volume:0.0}cm3, {otmId}={otm.Volume:0.0}cm3, IsEmpty={otm.IsEmpty}, HasSegment={otm.HasSegment}");
            }

            //==========================================================
            // MENSAGEM FINAL + LIMPEZA
            //==========================================================

            string msg = $"{ringStructure.Id} created. PTV usado no crop: {selectedStructure.Id}.\n\n" +
                         "Volumes:\n" + string.Join("\n", linhasVolume);

            if (falhas.Any())
                msg += "\n\nFalhas:\n" + string.Join("\n", falhas);

            MessageBox.Show(msg);

            // Se quiser fechar o paciente e resetar a tela ao final de tudo:
            // textBox1.Text = string.Empty;
            // clearComboBoxes();
            // comboBox4.ItemsSource = null;
            // comboBox4.SelectedItem = null;
            // currentPatient = null;
            // currentStructureSet = null;
            // currentCourse = null;
            // app.ClosePatient();
        }
    }
}