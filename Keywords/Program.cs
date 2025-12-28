using System.Text;
using Microsoft.Extensions.AI;
using MathNet.Numerics.LinearAlgebra;
using System.Globalization;
using OpenAI.Embeddings;
using dotenv.net;

class Program
{
    static void Main(string[] args)
    {
        var openAiKey = RequireEnv("OPENAI_API_KEY");
        Console.WriteLine("Using OpenAI API Key: " + openAiKey[..5] + "*****");
    }

    #region Helpers

    static string RequireEnv(string key)
    {
        // Load environment variables from .env file
        // DotEnv.Load();
        DotEnv.Load(new DotEnvOptions(
            probeForEnv: true,            // walk up from the current dir to find .env
            probeLevelsToSearch: 10,       // number of levels to walk up
            overwriteExistingVars: true    // overwrite if already set in the environment
        ));

        var v = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrEmpty(v))
        {
            throw new Exception($"Environment variable '{key}' is not set.");
        }
        return v!;
    }

    static void SaveCsv(
        List<(string Word, float[] Vector)> data,
        string filePath
    )
    {
        if (data.Count == 0)
        {
            Console.WriteLine("No vectors to project.");
            return;
        }

        // Build an n x d matrix (double) and mean-center
        int n = data.Count;
        int d = data[0].Vector.Length;

        var X = Matrix<double>.Build.Dense(n, d, (i, j) => data[i].Vector[j]);
        // Mean-center columns
        var means = Vector<double>.Build.Dense(d);
        for (int j = 0; j < d; j++)
        {
            means[j] = X.Column(j).Average();
            for (int i = 0; i < n; i++)
            {
                X[i, j] -= means[j];
            }
        }

        // PCA via SVD of mean-centered X
        // X = U * S * V^T, principal directions are columns of V
        var svd = X.Svd(computeVectors: true);
        var V = svd.VT.Transpose(); // d x d

        // Take first two principal components
        var V2 = V.SubMatrix(0, d, 0, 2); // d x 2
        var Y = X * V2; // n x 2

        // Write CSV: id,title,x,y (culture-invariant)
        using var sw = new StreamWriter(filePath, false, Encoding.UTF8);
        sw.WriteLine("id,title,x,y");
        
        for (int i = 0; i < n; i++)
        {
            var x = Y[i, 0];
            var y = Y[i, 1];
            sw.WriteLine(
                $"{i},{CsvEscape(data[i].Word)},{x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)}"
            );
        }
    }

    static string CsvEscape(string s)
    {
        if (s == null) return "";
        var needsQuotes = s.Contains('"') || s.Contains(',') || s.Contains('\n');
        if (needsQuotes)
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    #endregion Helpers
}