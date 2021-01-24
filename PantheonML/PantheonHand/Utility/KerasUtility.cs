using Keras.Layers;
using Keras.Models;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PantheonHand.Utility {
	public static class KerasUtility {
		public static BaseLayer Call(this BaseModel model, BaseLayer input = null) {
			return new BaseLayer(model.ToPython().InvokeMethod("call", input?.ToPython()));
		}

		public static void SetTrainable(this BaseModel model, bool trainable) {
			model.ToPython().SetAttr("trainable", new PyInt(trainable ? 1 : 0));
		}

		public static bool IsTrainable(this BaseModel model) {
			return model.ToPython().GetAttr("trainable").ToString() == "True";
		}

		public static BaseLayer ToLayer(this BaseModel model) {
			return new BaseLayer(model.ToPython());
		}
	}
}
