using System.IO;
using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.Testing
{
	public class CargoCountRecorder : MonoBehaviour
	{
		private StreamWriter writer;

		public float WriteInterval = 60f;

		public string FilePath = "C:\\temp\\asx\\cargo_counts.csv";

		private float nextWriteTime;

		private void Start()
		{
			CreateWriter();
		}

		private void CreateWriter()
		{
			if (!Directory.Exists(Path.GetDirectoryName(FilePath)))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
			}
			if (File.Exists(FilePath))
			{
				File.Delete(FilePath);
			}
			writer = new StreamWriter(File.OpenWrite(FilePath));
		}

		private void Update()
		{
			if (Time.time > nextWriteTime && EngineASX.LoadedAndReady)
			{
				WriteCargoCounts(writer);
				nextWriteTime = Time.time + WriteInterval;
			}
		}

		private void OnDestroy()
		{
			if (writer != null)
			{
				writer.Dispose();
			}
		}

		public void WriteCargoCounts(StreamWriter writer)
		{
			EngineASX instance = EngineASX.Instance;
			foreach (CargoClass cargoClass in instance.CargoClasses)
			{
				if (cargoClass.IsTraded && !cargoClass.IsReserved)
				{
					int cargoCount = instance.GetCargoCount(cargoClass);
					string value = $"{instance.ScenarioElapsedTime},{cargoClass.ClassName},{cargoCount}";
					writer.WriteLine(value);
				}
			}
		}
	}
}
