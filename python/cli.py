import argparse
import asyncio
from collections import deque

import cv2
import logging

import torch

from aiortc import (
    RTCIceCandidate,
    RTCPeerConnection,
    RTCSessionDescription,
)
from aiortc.contrib.signaling import BYE, add_signaling_arguments

from receiver import OpenCVReceiver
from signaler import UnityTcpSignaling

_LOGGER = logging.getLogger("mr.webrtc.python")
_LOGGER.addHandler(logging.NullHandler())


def send(message):
    _LOGGER.info("Receiver Channel > " + message)
    receiver.send_message(message)


async def run(pc, receiver, signaling, queue, render):
    @pc.on("track")
    def on_track(track):
        _LOGGER.info("Receiving %s" % track.kind)
        receiver.add_track(track)

    @pc.on("datachannel")
    def on_datachannel(channel):
        _LOGGER.info("Receiving %s" % channel)
        receiver.set_channel(channel)

    # load model
    model = torch.hub.load('ultralytics/yolov5', 'yolov5s')

    # connect signaling
    _LOGGER.info("Waiting for signaler connection ...")
    await signaling.connect()

    # consume signaling
    while True:
        obj = await signaling.receive()

        if isinstance(obj, RTCSessionDescription):
            await pc.setRemoteDescription(obj)
            await receiver.start()

            async def check_queue():
                while True:
                    if len(queue):
                        img = queue.pop()
                        queue.clear()
                        try:
                            result = model(img)

                            if render:
                                rendered = result.render()[0]

                                cv2.imshow("render", rendered)
                                cv2.waitKey(1)
                            else:
                                pandas = result.pandas()
                                xyxy = pandas.xyxy[0]
                                json = xyxy.to_json(orient="records")

                                send(json)

                        except Exception as e:
                            print(e)
                    await asyncio.sleep(0.2)

            asyncio.create_task(check_queue())

            if obj.type == "offer":
                # send answer
                await pc.setLocalDescription(await pc.createAnswer())
                await signaling.send(pc.localDescription)
        elif isinstance(obj, RTCIceCandidate):
            await pc.addIceCandidate(obj)
        elif obj is BYE:
            print("Exiting")
            break


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Video stream from the command line")
    parser.add_argument("--verbose", "-v", action="count")
    parser.add_argument("--host", "-ip", help="ip address of signaler/sender instance")
    parser.add_argument("--port", "-p", help="port of signaler/sender instance")
    parser.add_argument("--render", "-r", action="count", help="render the detection instead of sending it back")
    add_signaling_arguments(parser)
    args = parser.parse_args()

    if args.verbose:
        logging.basicConfig(level=logging.DEBUG)
    else:
        logging.basicConfig(level=logging.WARN)
        _LOGGER.setLevel(level=logging.INFO)

    host = args.host or "localhost"
    port = args.port or 9095

    # create signaling and peer connection
    signaling = UnityTcpSignaling(host=host, port=port)
    pc = RTCPeerConnection()

    frame_queue = deque()
    receiver = OpenCVReceiver(queue=frame_queue)
    # run event loop
    loop = asyncio.get_event_loop()
    try:
        loop.run_until_complete(
            run(
                pc=pc,
                receiver=receiver,
                signaling=signaling,
                queue=frame_queue,
                render=args.render,
            )
        )
    except KeyboardInterrupt:
        pass
    finally:
        # cleanup
        _LOGGER.info("Shutting down receiver and peer connection.")
        loop.run_until_complete(receiver.stop())
        loop.run_until_complete(signaling.close())
        loop.run_until_complete(pc.close())
