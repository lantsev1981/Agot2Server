using MyLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameService
{
    public class Synaptic
    {
        private static Random rnd = new Random(777);

        internal Perceptron Perceptron { get; set; }

        public string PerceptronId { get; private set; }
        public float Weight { get; set; }

        public Synaptic() { }

        internal Synaptic(Perceptron perceptron)
        {
            Perceptron = perceptron;
            PerceptronId = perceptron.Id;
            Weight = (float)rnd.NextDouble();
        }
    }

    public class Perceptron
    {
        [JsonIgnore]
        private static Func<List<Synaptic>, float> FX => (items) =>
        {
            items.ForEach(p => p.Perceptron.Math());
            float input = items.Sum(p => p.Perceptron.Output * p.Weight)/ (float)items.Count;
            return (float)(1 / (1 + System.Math.Pow(System.Math.E, -input)));
        };

        public List<Synaptic> Synaptics { get; set; }

        public string Id { get; set; }
        public float Output { get; set; }

        internal void Math()
        {
            if (Synaptics != null)
                Output = FX(Synaptics);
            else if (Output > 1 || Output < -1)
                Output = 1 / Output;
        }

        internal void Adjustment(float rate)
        {
            Synaptics?.ForEach(p =>
            {
                p.Weight += p.Perceptron.Output * rate;
                p.Perceptron.Adjustment(rate);
            });
        }
    }

    public class NeuroWeb
    {
        public List<Perceptron> InputPerceptrons { get; set; }
        public List<Perceptron> OutputPerceptrons { get; set; }

        public void AddInputPerceptron(string id, out Perceptron perceptron)
        {
            if (InputPerceptrons == null)
                InputPerceptrons = new List<Perceptron>();
            perceptron = InputPerceptrons.SingleOrDefault(p => p.Id == id);
            if (perceptron == null)
            {
                Perceptron inputPerceptron = perceptron = new Perceptron() { Id = id };
                InputPerceptrons.Add(inputPerceptron);

                OutputPerceptrons?.ForEach(p => p.Synaptics.Add(new Synaptic(inputPerceptron)));
            }
        }

        public void AddOutputPerceptron(string id, out Perceptron perceptron)
        {
            if (OutputPerceptrons == null)
                OutputPerceptrons = new List<Perceptron>();
            perceptron = OutputPerceptrons.SingleOrDefault(p => p.Id == id);
            if (perceptron == null)
            {
                Perceptron outputPerceptron = perceptron = new Perceptron() { Id = id, Synaptics = new List<Synaptic>() };
                OutputPerceptrons.Add(outputPerceptron);

                InputPerceptrons?.ForEach(p => outputPerceptron.Synaptics.Add(new Synaptic(p)));
            }
        }

        public void Math(List<float> trueValues = null)
        {
            OutputPerceptrons.ForEach(p => p.Math());
            for (int i = 0; i < trueValues?.Count; i++)
            {
                float error = trueValues[i] - OutputPerceptrons[i].Output;
                float rate = error * (OutputPerceptrons[i].Output * (1 - OutputPerceptrons[i].Output));

                OutputPerceptrons[i].Adjustment(rate);
            }
        }

        public void Load(string filename)
        {
            PublicFileJson<NeuroWeb> data = new PublicFileJson<NeuroWeb>(filename);
            data.Read();
            InputPerceptrons = data.Value?.InputPerceptrons;
            OutputPerceptrons = data.Value?.OutputPerceptrons;
            OutputPerceptrons?.ForEach(o => o.Synaptics.ForEach(s => s.Perceptron = InputPerceptrons.SingleOrDefault(i => i.Id == s.PerceptronId)));
        }
        public void Save(string filename)
        {
            PublicFileJson<NeuroWeb> data = new PublicFileJson<NeuroWeb>(filename) { Value = this };
            bool res = data.Write();
        }
    }

    public static class NeuroWebExt
    {
        public static void ClearOutputs(this List<Perceptron> items)
        {
            items.ForEach(p => p.Output = 0);
        }
    }
}
