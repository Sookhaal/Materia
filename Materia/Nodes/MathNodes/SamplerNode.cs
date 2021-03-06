﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Materia.Nodes.Attributes;
using Materia.Imaging.GLProcessing;
using Materia.Textures;
using Materia.MathHelpers;
using Materia.Imaging;
using Newtonsoft.Json;

namespace Materia.Nodes.MathNodes
{
    public class SamplerNode : MathNode
    {
        protected NodeInput input;
        protected NodeOutput output;

        protected int sampleIndex;

        [Title(Title = "Input")]
        [Dropdown(null, "Input0", "Input1")]
        public int SampleIndex
        {
            get
            {
                return sampleIndex;
            }
            set
            {
                sampleIndex = value;
                Updated();
            }
        }

        public SamplerNode(int w, int h, GraphPixelType p = GraphPixelType.RGBA)
        {
            CanPreview = false;

            Name = "Sampler";

            Id = Guid.NewGuid().ToString();
            shaderId = "S" + Id.Split('-')[0];

            input = new NodeInput(NodeType.Float2, this, "Pos");
            output = new NodeOutput(NodeType.Float4, this);

            sampleIndex = 0;

            Inputs = new List<NodeInput>();
            Inputs.Add(input);

            input.OnInputAdded += Input_OnInputAdded;
            input.OnInputChanged += Input_OnInputChanged;
            input.OnInputRemoved += Input_OnInputRemoved;

            Outputs = new List<NodeOutput>();
            Outputs.Add(output);
        }

        private void Input_OnInputRemoved(NodeInput n)
        {
            Updated();
        }

        private void Input_OnInputChanged(NodeInput n)
        {
            TryAndProcess();
        }

        private void Input_OnInputAdded(NodeInput n)
        {
            Updated();
        }

        public override void TryAndProcess()
        {
            if(input.HasInput)
            {
                Process();
            }
        }

        public override string GetShaderPart()
        {
            if (!input.HasInput) return "";
            var s = shaderId + "0";
            var n1id = (input.Input.Node as MathNode).ShaderId;

            var index = input.Input.Node.Outputs.IndexOf(input.Input);
            var samp = "Input" + sampleIndex;
            n1id += index;

            return "vec4 " + s + " = texture(" + samp + "," + n1id + ");\r\n";
        }

        void Process()
        {
            object inp = input.Input.Data;

            if (inp == null) return;

            /// This is to support FXMap cpu based
            /// samplers for properties
            if (ParentNode != null)
            {
                if(SampleIndex < ParentNode.Inputs.Count)
                {
                    var p = ParentNode.Inputs[SampleIndex];

                    if(p.HasInput)
                    {
                        GLTextuer2D i1 = (GLTextuer2D)p.Input.Data;

                        if (i1 == null || i1.Id == 0) return;

                        FloatBitmap bitmap = null;
                        
                        if(previewProcessor == null)
                        {
                            previewProcessor = new BasicImageRenderer();
                        }


                        MVector pos = (MVector)inp;

                        int dx = (int)Math.Abs(pos.X * i1.Width) % i1.Width;
                        int dy = (int)Math.Abs(pos.Y * i1.Height) % i1.Height;

                        previewProcessor.Process(i1.Width, i1.Height, i1);
                        float[] bits = previewProcessor.ReadFloat(dx, dy, 1, 1);
                        bitmap = new FloatBitmap(1, 1, bits);
                        previewProcessor.Complete();

                        float r, g, b, a;

                        bitmap.GetPixel(0, 0, out r, out g, out b, out a);

                        System.GC.Collect();

                        output.Data = new MVector(r, g, b, a);
                        output.Changed();

                        return;
                    }
                }
            }

            output.Data = new MVector();
            output.Changed();
        }

        public class SamplerNodeData : NodeData
        {
            public int sampleIndex;
        }

        public override string GetJson()
        {
            SamplerNodeData d = new SamplerNodeData();
            FillBaseNodeData(d);

            d.sampleIndex = sampleIndex;

            return JsonConvert.SerializeObject(d);
        }

        public override void FromJson(Dictionary<string, Node> nodes, string data)
        {
            SamplerNodeData d = JsonConvert.DeserializeObject<SamplerNodeData>(data);
            SetBaseNodeDate(d);

            sampleIndex = d.sampleIndex;

            SetConnections(nodes, d.outputs);

            TryAndProcess();
            Updated();
        }
    }
}
