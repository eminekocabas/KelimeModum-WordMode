using UnityEngine;
using XCharts.Runtime;

public class StatsGraphUI : MonoBehaviour
{
    public BarChart barChart;

    void OnEnable()
    {
        SetupChartAxis();
        RefreshGraph();
    }

    void SetupChartAxis()
    {
        // 1. Y Ekseni: Kategorileri 1'den 6'ya tanýmla
        var yAxis = barChart.EnsureChartComponent<YAxis>();
        yAxis.type = Axis.AxisType.Category;

        // Eksenin yönünü tersine çevirerek "1"i en baþa/büyük alana taþýyoruz

        yAxis.inverse = false;


        yAxis.data.Clear();
        yAxis.data.Add("");
        yAxis.data.Add("6. Tahmin");
        yAxis.data.Add("5. Tahmin");
        yAxis.data.Add("4. Tahmin");
        yAxis.data.Add("3. Tahmin");
        yAxis.data.Add("2. Tahmin");
        yAxis.data.Add("1. Tahmin");
        
        
        

        // 2. X Ekseni: Sayýsal deðer
        var xAxis = barChart.EnsureChartComponent<XAxis>();
        xAxis.type = Axis.AxisType.Value;

    }

    public void RefreshGraph()
    {
        if (barChart == null) return;

        // 1. Seriyi temizle
        var serie = barChart.GetSerie(0);
        if (serie == null) serie = barChart.AddSerie<Bar>("Galibiyetler");
        serie.ClearData();

        // 2. Ekseni (Category) manuel doldurmak için listeyi temizle
        var yAxis = barChart.EnsureChartComponent<YAxis>();
        yAxis.data.Clear();

        // 3. Verileri 6'dan 1'e doðru ekle
        int[] data = StatsService.Data.guessDistribution;
        yAxis.data.Add("");
        serie.AddData(0);

        for (int i = 5; i >= 0; i--)
        {
            // Eksen etiketini ekle
            yAxis.data.Add(i+1 + ". Tahmin");

            // Veriyi ekle
            serie.AddData(data[i]);
        }

        barChart.RefreshChart();
    }
}