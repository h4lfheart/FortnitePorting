class Log:
	INFO = u"\u001b[36m"
	WARNING = u"\u001b[31m"
	ERROR = u"\u001b[33m"
	RESET = u"\u001b[0m"

	@classmethod
	def info(cls, message):
		print(f"{Log.INFO}[INFO] {Log.RESET}{message}")

	@classmethod
	def warn(cls, message):
		print(f"{Log.WARNING}[WARN] {Log.RESET}{message}")
		
	@classmethod
	def error(cls, message):
		print(f"{Log.WARNING}[ERROR] {Log.RESET}{message}")