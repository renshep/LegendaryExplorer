#include "DirectXTex/DirectXTexP.h"

#include "TexConverter.h"

#include "DirectXTex/DirectXTex.h"
#include <locale>
#include <codecvt>
#include <string>

#define EXTENSION_DDS ".dds"
#define EXTENSION_PNG ".png"
#define EXTENSION_TGA ".tga"

#ifdef WIN32
#include <string.h>
#define strcasecmp _stricmp
#else
#include <strings.h>
#endif

std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>> StringConverter;
ID3D11Device* D3DDevice = nullptr;

const char* FindExtension(const char* filename) {
	const char* result = nullptr;
	const char* character = filename;
	while (*character != '\0') {
		if (*character == '.') {
			result = character;
		}
		character++;
	}
	return result;
}

HRESULT Initialize() {
	HRESULT hr = S_OK;

	hr = D3D11CreateDevice(nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, 0, nullptr, 0, D3D11_SDK_VERSION, &D3DDevice, nullptr, nullptr);
	
	return hr;
}

HRESULT Dispose() {

	D3DDevice->Release();
	
	return S_OK;
}

HRESULT ConvertTexture(const TextureBuffer* inputBuffer, TextureBuffer* outputBuffer) {

	HRESULT hr = S_OK;
	DirectX::Image sourceImage = { inputBuffer->Width, inputBuffer->Height, inputBuffer->Format, 0, 0, inputBuffer->PixelData };
	DirectX::ComputePitch(inputBuffer->Format, inputBuffer->Width, inputBuffer->Height, sourceImage.rowPitch, sourceImage.slicePitch, DirectX::CP_FLAGS::CP_FLAGS_NONE);

	outputBuffer->_ScratchImage = new DirectX::ScratchImage();

	// Determine what operations are needed for the conversion
	if (DirectX::IsCompressed(inputBuffer->Format)) {
		if (DirectX::IsCompressed(outputBuffer->Format)) {
			// Compressed -> Compressed Conversion
			// 
			// I won't implement this unless we really need it. It would decompress to an intermediate uncompressed image, and then recompress.
			return E_NOTIMPL;
		}
		else {
			// Decompression

			hr = DirectX::Decompress(sourceImage, outputBuffer->Format, *outputBuffer->_ScratchImage);
		}
	}
	else {
		if (DirectX::IsCompressed(outputBuffer->Format)) {
			// Compression

			if (outputBuffer->Format == DXGI_FORMAT_BC7_UNORM
				|| outputBuffer->Format == DXGI_FORMAT_BC7_UNORM_SRGB
				|| outputBuffer->Format == DXGI_FORMAT_BC6H_SF16
				|| outputBuffer->Format == DXGI_FORMAT_BC6H_UF16) {

				hr = DirectX::Compress(D3DDevice, sourceImage, outputBuffer->Format, DirectX::TEX_COMPRESS_FLAGS::TEX_COMPRESS_DEFAULT, 1.0f, *outputBuffer->_ScratchImage);
			}
			else {
				hr = DirectX::Compress(sourceImage, outputBuffer->Format, DirectX::TEX_COMPRESS_FLAGS::TEX_COMPRESS_PARALLEL, 1.0f, *outputBuffer->_ScratchImage);
			}
		}
		else {
			// Conversion

			hr = DirectX::Convert(sourceImage, outputBuffer->Format, DirectX::TEX_FILTER_FLAGS::TEX_FILTER_DEFAULT, DirectX::TEX_THRESHOLD_DEFAULT, *outputBuffer->_ScratchImage);
		}
	}

	if (FAILED(hr)) {
		//outputBuffer->_ScratchImage->Release();
		delete outputBuffer->_ScratchImage;
		return hr;
	}

	outputBuffer->PixelData = outputBuffer->_ScratchImage->GetPixels();
	outputBuffer->PixelDataLength = outputBuffer->_ScratchImage->GetPixelsSize();
	outputBuffer->Width = outputBuffer->_ScratchImage->GetMetadata().width;
	outputBuffer->Height = outputBuffer->_ScratchImage->GetMetadata().height;
	outputBuffer->Format = outputBuffer->_ScratchImage->GetMetadata().format;
	return hr;
}

HRESULT SaveTexture(const TextureBuffer* inputBuffer, const char* outputFilename) {

	if (inputBuffer == nullptr || inputBuffer->PixelData == nullptr || outputFilename == nullptr) {
		return E_INVALIDARG; // No buffer, no data in buffer, no filename
	}

	HRESULT hr = S_OK;

	DirectX::Image image = { inputBuffer->Width, inputBuffer->Height, inputBuffer->Format, 0, 0, inputBuffer->PixelData };
	DirectX::ComputePitch(inputBuffer->Format, inputBuffer->Width, inputBuffer->Height, image.rowPitch, image.slicePitch, DirectX::CP_FLAGS::CP_FLAGS_NONE);
	
	const char* extension = FindExtension(outputFilename);
	if (extension == nullptr) {
		hr = E_INVALIDARG; // No extension
	}
	else if (strcasecmp(extension, EXTENSION_DDS) == 0) {
		hr = DirectX::SaveToDDSFile(image, DirectX::DDS_FLAGS_NONE, StringConverter.from_bytes(outputFilename).c_str());
	}
	else if (strcasecmp(extension, EXTENSION_PNG) == 0) {
		// PNG files can't store compressed formats, so decompress before saving if necessary
		if (DirectX::IsCompressed(inputBuffer->Format)) {
			TextureBuffer decompressed = { };
			decompressed.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
			hr = ConvertTexture(inputBuffer, &decompressed);
			if (!FAILED(hr)) {
				hr = DirectX::SaveToWICFile(*decompressed._ScratchImage->GetImage(0, 0, 0), DirectX::WIC_FLAGS::WIC_FLAGS_NONE, DirectX::GetWICCodec(DirectX::WIC_CODEC_PNG), StringConverter.from_bytes(outputFilename).c_str());
				FreePixelData(&decompressed);
			}
		}
		else {
			hr = DirectX::SaveToWICFile(image, DirectX::WIC_FLAGS::WIC_FLAGS_NONE, DirectX::GetWICCodec(DirectX::WIC_CODEC_PNG), StringConverter.from_bytes(outputFilename).c_str());
		}
	}
	else if (strcasecmp(extension, EXTENSION_TGA) == 0) {
		// TGA files can't store compressed formats, so decompress before saving if necessary
		if (DirectX::IsCompressed(inputBuffer->Format)) {
			TextureBuffer decompressed = { };
			decompressed.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
			hr = ConvertTexture(inputBuffer, &decompressed);
			if (!FAILED(hr)) {
				hr = DirectX::SaveToTGAFile(*decompressed._ScratchImage->GetImage(0, 0, 0), StringConverter.from_bytes(outputFilename).c_str());
				FreePixelData(&decompressed);
			}
		}
		else {
			hr = DirectX::SaveToTGAFile(image, StringConverter.from_bytes(outputFilename).c_str());
		}
	}
	else {
		hr = E_INVALIDARG; // Unknown extension
	}

	return hr;
}

HRESULT LoadTexture(const char* inputFilename, TextureBuffer* outputBuffer) {

	if (inputFilename == nullptr || outputBuffer == nullptr || outputBuffer->PixelData != nullptr) {
		return E_INVALIDARG; // No filename, no output buffer, or output buffer already has data
	}

	outputBuffer->_ScratchImage = new DirectX::ScratchImage();

	HRESULT hr = S_OK;

	const char* extension = FindExtension(inputFilename);
	if (extension == nullptr) {
		hr = E_INVALIDARG; // No extension
	}
	else if (strcasecmp(extension, EXTENSION_DDS) == 0) {
		hr = DirectX::LoadFromDDSFile(StringConverter.from_bytes(inputFilename).c_str(), DirectX::DDS_FLAGS_NONE, nullptr, *outputBuffer->_ScratchImage);
	}
	else if (strcasecmp(extension, EXTENSION_PNG) == 0) {
		hr = DirectX::LoadFromWICFile(StringConverter.from_bytes(inputFilename).c_str(), DirectX::WIC_FLAGS::WIC_FLAGS_NONE, nullptr, *outputBuffer->_ScratchImage);
	}
	else if (strcasecmp(extension, EXTENSION_TGA) == 0) {
		hr = DirectX::LoadFromTGAFile(StringConverter.from_bytes(inputFilename).c_str(), nullptr, *outputBuffer->_ScratchImage);
	}
	else {
		hr = E_INVALIDARG; // Unknown extension
	}

	if (FAILED(hr)) {
		//outputBuffer->_ScratchImage->Release();
		delete outputBuffer->_ScratchImage;
		return hr;
	}

	// If the output buffer's format is UNKNOWN, then no conversion was requested
	if (outputBuffer->Format == DXGI_FORMAT_UNKNOWN || outputBuffer->Format == outputBuffer->_ScratchImage->GetMetadata().format) {
		outputBuffer->PixelData = outputBuffer->_ScratchImage->GetPixels();
		outputBuffer->PixelDataLength = outputBuffer->_ScratchImage->GetPixelsSize();
		outputBuffer->Width = outputBuffer->_ScratchImage->GetMetadata().width;
		outputBuffer->Height = outputBuffer->_ScratchImage->GetMetadata().height;
		outputBuffer->Format = outputBuffer->_ScratchImage->GetMetadata().format;
		return S_OK;
	}
	else {
		// Store the requested format before we overwrite the outputBuffer with the intermediate results
		TextureBuffer convertedTexture = { };
		convertedTexture.Format = outputBuffer->Format;

		outputBuffer->PixelData = outputBuffer->_ScratchImage->GetPixels();
		outputBuffer->PixelDataLength = outputBuffer->_ScratchImage->GetPixelsSize();
		outputBuffer->Width = outputBuffer->_ScratchImage->GetMetadata().width;
		outputBuffer->Height = outputBuffer->_ScratchImage->GetMetadata().height;
		outputBuffer->Format = outputBuffer->_ScratchImage->GetMetadata().format;

		hr = ConvertTexture(outputBuffer, &convertedTexture);
		if (FAILED(hr)) {
			// Destroy the intermediate image
			FreePixelData(outputBuffer);
			return hr;
		}

		// What the caller wanted is actually the converted image, so delete the intermediate image and return the converted image
		FreePixelData(outputBuffer);
		*outputBuffer = convertedTexture;

		return hr;
	}
}

HRESULT FreePixelData(TextureBuffer* textureBuffer) {
	
	if (textureBuffer == nullptr || textureBuffer->_ScratchImage == nullptr) {
		return E_INVALIDARG; // No buffer or no data in buffer
	}

	textureBuffer->_ScratchImage->Release();
	delete textureBuffer->_ScratchImage;
	textureBuffer->_ScratchImage = nullptr;
	textureBuffer->PixelData = nullptr;
	textureBuffer->PixelDataLength = 0;
	
	return S_OK;
}