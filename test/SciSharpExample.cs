﻿using System;
using System.Collections.Generic;
using System.Text;
using Tensorflow;
using Tensorflow.Keras.Engine;

namespace test
{
    public class SciSharpExample
    {
        public ExampleConfig Config { get; set; }

        public virtual void BuildModel()
        {

        }

        public virtual Graph BuildGraph()
        {
            throw new NotImplementedException();
        }

        public virtual Graph ImportGraph()
        {
            throw new NotImplementedException();
        }

        public virtual void PrepareData()
        {
            throw new NotImplementedException();
        }

        public virtual void Train()
        {
            
        }

        public virtual void Test()
        {

        }

        public virtual void Predict()
        {
        }

        public virtual string FreezeModel()
        {
            throw new NotImplementedException();
        }
    }
}
