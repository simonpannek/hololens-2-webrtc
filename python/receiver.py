import asyncio

from aiortc.mediastreams import MediaStreamError


class OpenCVReceiver:
    def __init__(self, queue):
        self.__tracks = []
        self.__channel = None
        self.__tasks = []
        self.queue = queue

    def add_track(self, track):
        self.__tracks.append(track)

    def set_channel(self, channel):
        self.__channel = channel

    def send_message(self, message):
        self.__channel.send(message)

    async def start(self):
        for track in self.__tracks:
            self.__tasks.append(asyncio.ensure_future(self.__run_track(track)))

    async def stop(self):
        for task in self.__tasks:
            task.cancel()

    async def __run_track(self, track):
        while True:
            try:
                frame = await track.recv()
                self.queue.append(frame.to_ndarray(format="bgr24"))
            except MediaStreamError:
                pass
